using Novolis.Economy;
using Novolis.Economy.Production;

namespace Novolis.Economy.Logistics;

/// <summary>Advances and creates shipments (legacy single-leg and multi-leg hub network).</summary>
public static class LogisticsEngine
{
  /// <summary>
  /// Hours a multi-leg shipment may remain stuck at a hub waiting for fuel or toll payment
  /// before cargo is unloaded in place and the shipment cancelled.
  /// </summary>
  public const int MaxHubStallHours = 48;

  /// <summary>Legacy: pulls stock and creates an in-transit shipment on a FreightRoute.</summary>
  public static ActiveShipment? TryDepart(
    InventoryStore inventory,
    FirmId firmId,
    FreightRoute route,
    ProductId productId,
    Quantity quantity,
    SimulationHour now,
    out Money unitCost)
  {
    unitCost = Money.Zero;
    var key = new InventoryKey(firmId, route.Origin, productId);
    if (!inventory.TryTake(key, quantity, out _, out var totalCost) || quantity.Value <= 0m)
    {
      return null;
    }

    unitCost = Money.From(totalCost.Amount / quantity.Value);
    return new ActiveShipment(
      ShipmentId.From(CreateShipmentGuid(firmId, now, productId, quantity)),
      firmId,
      route.Id,
      productId,
      quantity,
      unitCost,
      Math.Max(1, route.TransitHours),
      now);
  }

  /// <summary>
  /// Multi-leg: pull cargo at origin hub, begin loading dwell, bunker for first leg when fuel is configured.
  /// Pass quantity zero for empty reposition (no inventory take).
  /// </summary>
  public static ActiveShipment? TryDepartItinerary(
    InventoryStore inventory,
    FirmId firmId,
    TransportHub originHub,
    Itinerary itinerary,
    VehicleClass vehicle,
    ProductId productId,
    Quantity quantity,
    ProductId? fuelProductId,
    SimulationHour now,
    IReadOnlyDictionary<TransportCorridorId, TransportCorridor> corridors,
    out Money unitCost,
    out string? failReason,
    TransitProfile profile = TransitProfile.StandardCommercial)
  {
    unitCost = Money.Zero;
    failReason = null;

    if (itinerary.LegCount == 0)
    {
      failReason = "empty-itinerary";
      return null;
    }

    if (quantity.Value > vehicle.CargoCapacity.Value)
    {
      failReason = "cargo-exceeds-vehicle";
      return null;
    }

    var firstCorridor = corridors[itinerary.CorridorIds[0]];
    if (quantity.Value > firstCorridor.MaxCargo.Value)
    {
      failReason = "cargo-exceeds-corridor";
      return null;
    }

    var cargoKey = new InventoryKey(firmId, originHub.LocationId, productId);
    Money totalCost = Money.Zero;
    if (quantity.Value > 0m)
    {
      if (!inventory.TryTake(cargoKey, quantity, out _, out totalCost))
      {
        failReason = "cargo-unavailable";
        return null;
      }

      unitCost = Money.From(totalCost.Amount / quantity.Value);
    }

    var shipment = new ActiveShipment(
      ShipmentId.From(CreateShipmentGuid(firmId, now, productId, quantity)),
      firmId,
      productId,
      quantity,
      unitCost,
      now,
      itinerary,
      vehicle,
      originHub.Id,
      fuelProductId)
    {
      Phase = ShipmentPhase.Loading,
      SegmentHoursRemaining = Math.Max(0, originHub.DwellHours),
      PlannedLegBurn = Quantity.Zero,
      LegHoursTotal = 0,
      TransitProfile = profile,
    };

    if (fuelProductId is { } fuelId)
    {
      var needed = ItineraryPlanner.FuelBurnForLeg(firstCorridor, vehicle, profile);
      if (!TryBunker(inventory, firmId, originHub, fuelId, needed, vehicle, shipment, out _))
      {
        if (quantity.Value > 0m)
        {
          inventory.Add(
            cargoKey,
            new ProductBatch(
              productId,
              quantity,
              new ProductQuality(100m),
              unitCost,
              now.Date,
              BrandId: null));
        }

        failReason = "fuel-unavailable";
        return null;
      }

      // Fill remaining tank capacity when origin stock allows — fewer mid-hub bunkers.
      var room = Quantity.From(Math.Max(0m, vehicle.FuelTankCapacity.Value - shipment.OnboardFuel.Value));
      if (room.Value > 0m)
      {
        TryBunker(inventory, firmId, originHub, fuelId, room, vehicle, shipment, out _);
      }
    }

    if (shipment.SegmentHoursRemaining == 0)
    {
      Money tolls = Money.Zero;
      BeginLegOrWait(shipment, corridors, hubs: null, berthUsage: null, canPayToll: null, ref tolls);
    }

    return shipment;
  }

  /// <summary>Ticks all shipments one hour.</summary>
  public static LogisticsTickResult AdvanceHour(
    IList<ActiveShipment> shipments,
    InventoryStore inventory,
    IReadOnlyDictionary<FreightRouteId, FreightRoute> routes,
    IReadOnlyDictionary<TransportHubId, TransportHub>? hubs = null,
    IReadOnlyDictionary<TransportCorridorId, TransportCorridor>? corridors = null,
    Dictionary<TransportHubId, int>? berthUsage = null,
    Func<FirmId, Money, bool>? tryPayToll = null,
    Money fuelUnitCost = default)
  {
    hubs ??= new Dictionary<TransportHubId, TransportHub>();
    corridors ??= new Dictionary<TransportCorridorId, TransportCorridor>();
    berthUsage ??= new Dictionary<TransportHubId, int>();
    if (fuelUnitCost.Amount <= 0m)
    {
      fuelUnitCost = Money.From(1m);
    }

    var delivered = new List<ActiveShipment>();
    var crewByFirm = new Dictionary<FirmId, decimal>();
    var fuelBurned = Quantity.Zero;
    var fuelBurnValue = Money.Zero;
    var fuelBurnByFirm = new Dictionary<FirmId, Money>();
    var tollsPaid = Money.Zero;
    var fuelBunkered = Quantity.Zero;
    var driveWear = 0m;
    var legStarts = new List<(ActiveShipment Shipment, TransportCorridorId CorridorId)>();
    var hubArrivals = new List<(ActiveShipment Shipment, TransportHubId HubId)>();

    foreach (var shipment in shipments
               .Where(s => s.Status == ShipmentStatus.InTransit)
               .OrderBy(s => s.Id.Value))
    {
      shipment.CrewLaborThisTick = 0m;

      if (shipment.IsLegacy)
      {
        AdvanceLegacy(shipment, inventory, routes, delivered);
        continue;
      }

      if (shipment.Vehicle is null || corridors.Count == 0 || hubs.Count == 0)
      {
        shipment.Phase = ShipmentPhase.Cancelled;
        shipment.Status = ShipmentStatus.Cancelled;
        continue;
      }

      switch (shipment.Phase)
      {
        case ShipmentPhase.WaitingBerth:
          if (!TryClaimBerth(shipment.CurrentHubId, hubs, berthUsage))
          {
            break;
          }

          if (shipment.LegIndex >= shipment.Itinerary.LegCount)
          {
            StartFinalUnload(shipment, hubs, inventory, delivered);
          }
          else
          {
            if (!EnsureFuelForNextLeg(
                  shipment, inventory, hubs, corridors, fuelUnitCost, ref fuelBunkered))
            {
              DeferAtHubForFuelOrToll(shipment, inventory, hubs, delivered);
              break;
            }

            BeginLeg(shipment, corridors, tryPayToll, ref tollsPaid, legStarts);
            if (shipment.Phase == ShipmentPhase.Underway)
            {
              shipment.HubStallHours = 0;
            }
            else if (shipment.Phase == ShipmentPhase.Loading)
            {
              DeferAtHubForFuelOrToll(shipment, inventory, hubs, delivered);
            }
          }

          break;

        case ShipmentPhase.Loading:
        case ShipmentPhase.Unloading:
          if (shipment.SegmentHoursRemaining > 0)
          {
            shipment.SegmentHoursRemaining--;
          }

          if (shipment.SegmentHoursRemaining > 0)
          {
            break;
          }

          if (shipment.Phase == ShipmentPhase.Unloading && shipment.LegIndex >= shipment.Itinerary.LegCount)
          {
            CompleteDelivery(shipment, inventory, hubs, delivered);
            break;
          }

          if (shipment.LegIndex < shipment.Itinerary.LegCount)
          {
            if (!EnsureFuelForNextLeg(
                  shipment, inventory, hubs, corridors, fuelUnitCost, ref fuelBunkered))
            {
              if (DeferAtHubForFuelOrToll(shipment, inventory, hubs, delivered))
              {
                break;
              }

              break;
            }

            BeginLegOrWait(shipment, corridors, hubs, berthUsage, tryPayToll, ref tollsPaid, legStarts);
            if (shipment.Phase == ShipmentPhase.Underway)
            {
              shipment.HubStallHours = 0;
            }
            else if (shipment.Phase == ShipmentPhase.Loading
                     && DeferAtHubForFuelOrToll(shipment, inventory, hubs, delivered))
            {
              break;
            }
          }

          break;

        case ShipmentPhase.Underway:
        {
          var crew = shipment.Vehicle.CrewLaborPerUnderwayHour;
          shipment.CrewLaborThisTick = crew;
          crewByFirm[shipment.FirmId] = crewByFirm.GetValueOrDefault(shipment.FirmId) + crew;

          var loadFrac = shipment.Vehicle.CargoCapacity.Value <= 0m
            ? 1m
            : shipment.Quantity.Value / shipment.Vehicle.CargoCapacity.Value;
          var legDifficulty = 1m;
          if (shipment.Itinerary.LegCount > 0
              && shipment.LegIndex < shipment.Itinerary.LegCount
              && corridors.TryGetValue(shipment.Itinerary.CorridorIds[shipment.LegIndex], out var wearCor))
          {
            legDifficulty = wearCor.Difficulty;
          }

          var wearTick = TransitProfiles.WearForUnderwayHour(
            shipment.TransitProfile, loadFrac, legDifficulty);
          shipment.DriveWearAccrued += wearTick;
          driveWear += wearTick;

          if (shipment.FuelProductId is not null && shipment.LegHoursTotal > 0)
          {
            var burnThisHour = Quantity.From(shipment.PlannedLegBurn.Value / shipment.LegHoursTotal);
            if (shipment.SegmentHoursRemaining <= 1)
            {
              var burnedSoFar = Quantity.From(
                shipment.PlannedLegBurn.Value * (shipment.LegHoursTotal - shipment.SegmentHoursRemaining) /
                shipment.LegHoursTotal);
              burnThisHour = Quantity.From(Math.Max(0m, shipment.PlannedLegBurn.Value - burnedSoFar.Value));
            }

            burnThisHour = Quantity.From(Math.Min(burnThisHour.Value, shipment.OnboardFuel.Value));
            shipment.OnboardFuel = Quantity.From(Math.Max(0m, shipment.OnboardFuel.Value - burnThisHour.Value));
            fuelBurned = Quantity.From(fuelBurned.Value + burnThisHour.Value);
            var burnValue = Money.From(burnThisHour.Value * fuelUnitCost.Amount);
            fuelBurnValue = Money.From(fuelBurnValue.Amount + burnValue.Amount);
            if (burnValue.Amount > 0m)
            {
              fuelBurnByFirm[shipment.FirmId] =
                Money.From(fuelBurnByFirm.GetValueOrDefault(shipment.FirmId).Amount + burnValue.Amount);
            }
          }

          shipment.SegmentHoursRemaining--;
          if (shipment.SegmentHoursRemaining > 0)
          {
            break;
          }

          var arrivedCorridor = corridors[shipment.Itinerary.CorridorIds[shipment.LegIndex]];
          shipment.CurrentHubId = arrivedCorridor.To;
          shipment.LegIndex++;
          hubArrivals.Add((shipment, shipment.CurrentHubId));

          if (shipment.LegIndex >= shipment.Itinerary.LegCount)
          {
            StartFinalUnload(shipment, hubs, inventory, delivered);
          }
          else
          {
            shipment.Phase = ShipmentPhase.Unloading;
            shipment.SegmentHoursRemaining = Math.Max(0, hubs[shipment.CurrentHubId].DwellHours);
            if (shipment.SegmentHoursRemaining == 0)
            {
              if (!EnsureFuelForNextLeg(
                    shipment, inventory, hubs, corridors, fuelUnitCost, ref fuelBunkered))
              {
                DeferAtHubForFuelOrToll(shipment, inventory, hubs, delivered);
                break;
              }

              BeginLegOrWait(shipment, corridors, hubs, berthUsage, tryPayToll, ref tollsPaid, legStarts);
              if (shipment.Phase == ShipmentPhase.Underway)
              {
                shipment.HubStallHours = 0;
              }
              else if (shipment.Phase == ShipmentPhase.Loading)
              {
                DeferAtHubForFuelOrToll(shipment, inventory, hubs, delivered);
              }
            }
          }

          break;
        }
      }
    }

    return new LogisticsTickResult(
      delivered,
      crewByFirm,
      fuelBurned,
      fuelBurnValue,
      fuelBurnByFirm,
      tollsPaid,
      fuelBunkered,
      legStarts,
      hubArrivals,
      driveWear);
  }

  /// <summary>
  /// Counts a fuel/toll stall hour. After <see cref="MaxHubStallHours"/>, unloads cargo at the
  /// current hub and cancels so the hull can resume work.
  /// </summary>
  /// <returns>True when the shipment was abandoned.</returns>
  private static bool DeferAtHubForFuelOrToll(
    ActiveShipment shipment,
    InventoryStore inventory,
    IReadOnlyDictionary<TransportHubId, TransportHub> hubs,
    List<ActiveShipment> delivered)
  {
    _ = delivered;
    shipment.HubStallHours++;
    if (shipment.HubStallHours >= MaxHubStallHours)
    {
      AbandonAtCurrentHub(shipment, inventory, hubs);
      return true;
    }

    shipment.Phase = ShipmentPhase.Loading;
    shipment.SegmentHoursRemaining = 1;
    return false;
  }

  private static void AbandonAtCurrentHub(
    ActiveShipment shipment,
    InventoryStore inventory,
    IReadOnlyDictionary<TransportHubId, TransportHub> hubs)
  {
    var hub = hubs[shipment.CurrentHubId];
    inventory.Add(
      new InventoryKey(shipment.FirmId, hub.LocationId, shipment.ProductId),
      new ProductBatch(
        shipment.ProductId,
        shipment.Quantity,
        new ProductQuality(100m),
        shipment.UnitCost,
        shipment.DepartedAt.Date,
        BrandId: null));
    shipment.Status = ShipmentStatus.Cancelled;
    shipment.Phase = ShipmentPhase.Cancelled;
    shipment.HubStallHours = 0;
  }

  private static bool EnsureFuelForNextLeg(
    ActiveShipment shipment,
    InventoryStore inventory,
    IReadOnlyDictionary<TransportHubId, TransportHub> hubs,
    IReadOnlyDictionary<TransportCorridorId, TransportCorridor> corridors,
    Money fuelUnitCost,
    ref Quantity fuelBunkered)
  {
    _ = fuelUnitCost;
    if (shipment.FuelProductId is not { } fuelId || shipment.Vehicle is null)
    {
      return true;
    }

    var next = corridors[shipment.Itinerary.CorridorIds[shipment.LegIndex]];
    var burn = ItineraryPlanner.FuelBurnForLeg(next, shipment.Vehicle, shipment.TransitProfile);
    var deficit = Quantity.From(Math.Max(0m, burn.Value - shipment.OnboardFuel.Value));
    if (deficit.Value > 0m)
    {
      if (!TryBunker(
            inventory,
            shipment.FirmId,
            hubs[shipment.CurrentHubId],
            fuelId,
            deficit,
            shipment.Vehicle,
            shipment,
            out var bunkered))
      {
        return false;
      }

      fuelBunkered = Quantity.From(fuelBunkered.Value + bunkered.Value);
    }

    // Opportunistic top-up so later legs need fewer mid-hub bunkers.
    var room = Quantity.From(Math.Max(0m, shipment.Vehicle.FuelTankCapacity.Value - shipment.OnboardFuel.Value));
    if (room.Value > 0m
        && TryBunker(
          inventory,
          shipment.FirmId,
          hubs[shipment.CurrentHubId],
          fuelId,
          room,
          shipment.Vehicle,
          shipment,
          out var topped))
    {
      fuelBunkered = Quantity.From(fuelBunkered.Value + topped.Value);
    }

    return true;
  }

  private static void StartFinalUnload(
    ActiveShipment shipment,
    IReadOnlyDictionary<TransportHubId, TransportHub> hubs,
    InventoryStore inventory,
    List<ActiveShipment> delivered)
  {
    shipment.Phase = ShipmentPhase.Unloading;
    shipment.SegmentHoursRemaining = Math.Max(0, hubs[shipment.CurrentHubId].DwellHours);
    if (shipment.SegmentHoursRemaining == 0)
    {
      CompleteDelivery(shipment, inventory, hubs, delivered);
    }
  }

  private static void AdvanceLegacy(
    ActiveShipment shipment,
    InventoryStore inventory,
    IReadOnlyDictionary<FreightRouteId, FreightRoute> routes,
    List<ActiveShipment> delivered)
  {
    shipment.HoursRemaining--;
    if (shipment.HoursRemaining > 0)
    {
      return;
    }

    if (!routes.TryGetValue(shipment.RouteId, out var route))
    {
      shipment.Status = ShipmentStatus.Cancelled;
      return;
    }

    var accepted = inventory.Add(
      new InventoryKey(shipment.FirmId, route.Destination, shipment.ProductId),
      new ProductBatch(
        shipment.ProductId,
        shipment.Quantity,
        new ProductQuality(100m),
        shipment.UnitCost,
        shipment.DepartedAt.Date,
        BrandId: null));
    if (accepted.Value < shipment.Quantity.Value)
    {
      shipment.Quantity = accepted;
    }

    shipment.Status = ShipmentStatus.Delivered;
    shipment.Phase = ShipmentPhase.Delivered;
    delivered.Add(shipment);
  }

  private static void CompleteDelivery(
    ActiveShipment shipment,
    InventoryStore inventory,
    IReadOnlyDictionary<TransportHubId, TransportHub> hubs,
    List<ActiveShipment> delivered)
  {
    if (shipment.Quantity.Value <= 0m)
    {
      // Empty reposition — arrive with no cargo to unload.
      shipment.Status = ShipmentStatus.Delivered;
      shipment.Phase = ShipmentPhase.Delivered;
      delivered.Add(shipment);
      return;
    }

    var hub = hubs[shipment.CurrentHubId];
    var key = new InventoryKey(shipment.FirmId, hub.LocationId, shipment.ProductId);
    // Hard store-limit: wait in unload rather than destroy cargo.
    if (inventory.Limits.TryGetHard(hub.LocationId, shipment.ProductId, out _))
    {
      var room = inventory.Limits.Room(inventory, hub.LocationId, shipment.ProductId);
      if (room + 0.0000001m < shipment.Quantity.Value)
      {
        shipment.Phase = ShipmentPhase.Unloading;
        shipment.SegmentHoursRemaining = Math.Max(1, hub.DwellHours);
        return;
      }
    }

    var accepted = inventory.Add(
      key,
      new ProductBatch(
        shipment.ProductId,
        shipment.Quantity,
        new ProductQuality(100m),
        shipment.UnitCost,
        shipment.DepartedAt.Date,
        BrandId: null));
    if (accepted.Value + 0.0000001m < shipment.Quantity.Value)
    {
      shipment.Phase = ShipmentPhase.Unloading;
      shipment.SegmentHoursRemaining = Math.Max(1, hub.DwellHours);
      return;
    }

    shipment.Status = ShipmentStatus.Delivered;
    shipment.Phase = ShipmentPhase.Delivered;
    delivered.Add(shipment);
  }

  private static void BeginLegOrWait(
    ActiveShipment shipment,
    IReadOnlyDictionary<TransportCorridorId, TransportCorridor> corridors,
    IReadOnlyDictionary<TransportHubId, TransportHub>? hubs,
    Dictionary<TransportHubId, int>? berthUsage,
    Func<FirmId, Money, bool>? canPayToll,
    ref Money tollsPaid,
    List<(ActiveShipment Shipment, TransportCorridorId CorridorId)>? legStarts = null)
  {
    if (hubs is not null && berthUsage is not null && !TryClaimBerth(shipment.CurrentHubId, hubs, berthUsage))
    {
      shipment.Phase = ShipmentPhase.WaitingBerth;
      return;
    }

    BeginLeg(shipment, corridors, canPayToll, ref tollsPaid, legStarts);
  }

  private static void BeginLeg(
    ActiveShipment shipment,
    IReadOnlyDictionary<TransportCorridorId, TransportCorridor> corridors,
    Func<FirmId, Money, bool>? tryPayToll,
    ref Money tollsPaid,
    List<(ActiveShipment Shipment, TransportCorridorId CorridorId)>? legStarts)
  {
    var corridorId = shipment.Itinerary.CorridorIds[shipment.LegIndex];
    var corridor = corridors[corridorId];
    var burn = ItineraryPlanner.FuelBurnForLeg(corridor, shipment.Vehicle!, shipment.TransitProfile);
    if (shipment.FuelProductId is not null && shipment.OnboardFuel.Value + 0.0000001m < burn.Value)
    {
      shipment.Phase = ShipmentPhase.Loading;
      shipment.SegmentHoursRemaining = 1;
      return;
    }

    if (corridor.Toll.Amount > 0m)
    {
      if (tryPayToll is null || !tryPayToll(shipment.FirmId, corridor.Toll))
      {
        shipment.Phase = ShipmentPhase.Loading;
        shipment.SegmentHoursRemaining = 1;
        return;
      }

      tollsPaid = Money.From(tollsPaid.Amount + corridor.Toll.Amount);
    }

    shipment.Phase = ShipmentPhase.Underway;
    shipment.LegHoursTotal = TransitProfiles.EffectiveHours(corridor, shipment.TransitProfile);
    shipment.SegmentHoursRemaining = shipment.LegHoursTotal;
    shipment.PlannedLegBurn = burn;
    legStarts?.Add((shipment, corridorId));
  }

  private static bool TryClaimBerth(
    TransportHubId hubId,
    IReadOnlyDictionary<TransportHubId, TransportHub> hubs,
    Dictionary<TransportHubId, int> berthUsage)
  {
    if (!hubs.TryGetValue(hubId, out var hub) || hub.BerthCapacity <= 0)
    {
      return true;
    }

    var used = berthUsage.GetValueOrDefault(hubId);
    if (used >= hub.BerthCapacity)
    {
      return false;
    }

    berthUsage[hubId] = used + 1;
    return true;
  }

  private static bool TryBunker(
    InventoryStore inventory,
    FirmId firmId,
    TransportHub hub,
    ProductId fuelProductId,
    Quantity needed,
    VehicleClass vehicle,
    ActiveShipment shipment,
    out Quantity bunkered)
  {
    bunkered = Quantity.Zero;
    if (needed.Value <= 0m)
    {
      return true;
    }

    var room = Quantity.From(Math.Max(0m, vehicle.FuelTankCapacity.Value - shipment.OnboardFuel.Value));
    var buy = Quantity.From(Math.Min(needed.Value, room.Value));
    if (buy.Value <= 0m)
    {
      return shipment.OnboardFuel.Value + 0.0000001m >= needed.Value;
    }

    var key = new InventoryKey(firmId, hub.LocationId, fuelProductId);
    if (!inventory.TryTake(key, buy, out _, out _))
    {
      return false;
    }

    shipment.OnboardFuel = Quantity.From(shipment.OnboardFuel.Value + buy.Value);
    bunkered = buy;
    return shipment.OnboardFuel.Value + 0.0000001m >= needed.Value;
  }

  private static Guid CreateShipmentGuid(FirmId firmId, SimulationHour now, ProductId productId, Quantity qty)
  {
    var bytes = firmId.Value.ToByteArray();
    var hour = BitConverter.GetBytes(now.HourIndex);
    Buffer.BlockCopy(hour, 0, bytes, 8, 4);
    bytes[12] = (byte)productId.Value.GetHashCode();
    bytes[13] = (byte)qty.Value;
    bytes[14] = 0x51;
    bytes[15] = 0x1F;
    return new Guid(bytes);
  }
}

/// <summary>Aggregates from one logistics hour.</summary>
public sealed record LogisticsTickResult(
  IReadOnlyList<ActiveShipment> Delivered,
  IReadOnlyDictionary<FirmId, decimal> CrewLaborByFirm,
  Quantity FuelBurned,
  Money FuelBurnValue,
  IReadOnlyDictionary<FirmId, Money> FuelBurnValueByFirm,
  Money TollsPaid,
  Quantity FuelBunkered,
  IReadOnlyList<(ActiveShipment Shipment, TransportCorridorId CorridorId)> LegStarts,
  IReadOnlyList<(ActiveShipment Shipment, TransportHubId HubId)> HubArrivals,
  decimal DriveWear = 0m);
