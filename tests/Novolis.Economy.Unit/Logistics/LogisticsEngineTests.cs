using System.Collections.Immutable;
using Novolis.Economy;
using Novolis.Economy.Logistics;
using Novolis.Economy.Production;

namespace Novolis.Economy.Unit.Logistics;

public sealed class LogisticsEngineTests
{
    private static readonly SimulationHour Now = SimulationHour.Epoch;

    [Test]
    public async Task TryDepart_FailsWithoutStock()
    {
        var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000001"));
        var loc = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-000000000002"));
        var product = ProductId.From(Guid.Parse("00000000-0000-4000-8000-000000000003"));
        var route = new FreightRoute(
            FreightRouteId.From(Guid.Parse("00000000-0000-4000-8000-000000000004")),
            loc,
            InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-000000000005")),
            TransitHours: 2,
            Capacity: Quantity.From(100m));

        var inventory = new InventoryStore();
        var shipment = LogisticsEngine.TryDepart(
            inventory, firm, route, product, Quantity.From(5m), Now, out var unitCost);

        await Assert.That(shipment).IsNull();
        await Assert.That(unitCost.Amount).IsEqualTo(0m);
    }

    [Test]
    public async Task TryDepart_SucceedsWithStock()
    {
        var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000011"));
        var loc = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-000000000012"));
        var dest = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-000000000013"));
        var product = ProductId.From(Guid.Parse("00000000-0000-4000-8000-000000000014"));
        var route = new FreightRoute(
            FreightRouteId.From(Guid.Parse("00000000-0000-4000-8000-000000000015")),
            loc, dest, TransitHours: 3, Capacity: Quantity.From(100m));

        var inventory = new InventoryStore();
        inventory.Add(
            new InventoryKey(firm, loc, product),
            new ProductBatch(product, Quantity.From(10m), new ProductQuality(100m), Money.From(2m), SimulationDate.Epoch, null),
            bypassLimits: true);

        var shipment = LogisticsEngine.TryDepart(
            inventory, firm, route, product, Quantity.From(4m), Now, out var unitCost);

        await Assert.That(shipment).IsNotNull();
        await Assert.That(unitCost.Amount).IsEqualTo(2m);
        await Assert.That(inventory.GetQuantity(new InventoryKey(firm, loc, product)).Value).IsEqualTo(6m);
    }

    [Test]
    public async Task TryDepartItinerary_RejectsEmptyAndOversizeCargo()
    {
        var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000021"));
        var loc = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-000000000022"));
        var hub = new TransportHub(TransportHubId.From(Guid.Parse("00000000-0000-4000-8000-000000000023")), loc, "H", 1, 2);
        var product = ProductId.From(Guid.Parse("00000000-0000-4000-8000-000000000024"));
        var vehicle = new VehicleClass(
            VehicleClassId.From(Guid.Parse("00000000-0000-4000-8000-000000000025")),
            CargoCapacity: Quantity.From(5m),
            FuelBurnPerDifficultyHour: 1m,
            CrewLaborPerUnderwayHour: 1m,
            FuelTankCapacity: Quantity.From(10m));
        var inventory = new InventoryStore();
        var corridors = ImmutableDictionary<TransportCorridorId, TransportCorridor>.Empty;

        var empty = LogisticsEngine.TryDepartItinerary(
            inventory, firm, hub, Itinerary.Empty, vehicle, product, Quantity.From(1m),
            fuelProductId: null, Now, corridors, out _, out var reason1);
        await Assert.That(empty).IsNull();
        await Assert.That(reason1).IsEqualTo("empty-itinerary");

        var corridor = new TransportCorridor(
            TransportCorridorId.From(Guid.Parse("00000000-0000-4000-8000-000000000026")),
            hub.Id, TransportHubId.From(Guid.Parse("00000000-0000-4000-8000-000000000027")),
            TransitHours: 2, MaxCargo: Quantity.From(10m), Difficulty: 1m, Toll: Money.Zero);
        corridors = corridors.Add(corridor.Id, corridor);
        await Assert.That(ItineraryPlanner.TryPlan(
            hub.Id, corridor.To, Quantity.From(1m), vehicle, corridors, out var itinerary)).IsTrue();

        var oversize = LogisticsEngine.TryDepartItinerary(
            inventory, firm, hub, itinerary, vehicle, product, Quantity.From(6m),
            fuelProductId: null, Now, corridors, out _, out var reason2);
        await Assert.That(oversize).IsNull();
        await Assert.That(reason2).IsEqualTo("cargo-exceeds-vehicle");
    }

    [Test]
    public async Task TryDepartItinerary_FailsWhenFuelUnavailable()
    {
        var (inventory, firm, hub, product, fuel, vehicle, corridors, itinerary) = BuildItineraryWorld();
        inventory.Add(
            new InventoryKey(firm, hub.LocationId, product),
            new ProductBatch(product, Quantity.From(5m), new ProductQuality(100m), Money.From(2m), SimulationDate.Epoch, null),
            bypassLimits: true);

        var fail = LogisticsEngine.TryDepartItinerary(
            inventory, firm, hub, itinerary, vehicle, product, Quantity.From(3m),
            fuel, Now, corridors, out _, out var reason);

        await Assert.That(fail).IsNull();
        await Assert.That(reason).IsEqualTo("fuel-unavailable");
        await Assert.That(inventory.GetQuantity(new InventoryKey(firm, hub.LocationId, product)).Value).IsEqualTo(5m);
    }

    [Test]
    public async Task TryDepartItinerary_SucceedsWithCargoAndFuel()
    {
        var (inventory, firm, hub, product, fuel, vehicle, corridors, itinerary) = BuildItineraryWorld();
        inventory.Add(
            new InventoryKey(firm, hub.LocationId, product),
            new ProductBatch(product, Quantity.From(5m), new ProductQuality(100m), Money.From(2m), SimulationDate.Epoch, null),
            bypassLimits: true);
        inventory.Add(
            new InventoryKey(firm, hub.LocationId, fuel),
            new ProductBatch(fuel, Quantity.From(20m), new ProductQuality(100m), Money.From(1m), SimulationDate.Epoch, null),
            bypassLimits: true);

        var shipment = LogisticsEngine.TryDepartItinerary(
            inventory, firm, hub, itinerary, vehicle, product, Quantity.From(3m),
            fuel, Now, corridors, out var unitCost, out var reason);

        await Assert.That(shipment).IsNotNull();
        await Assert.That(reason).IsNull();
        await Assert.That(unitCost.Amount).IsEqualTo(2m);
        await Assert.That(shipment!.Phase).IsEqualTo(ShipmentPhase.Loading);
    }

    [Test]
    public async Task AdvanceHour_DeliversLegacyShipment()
    {
        var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000031"));
        var loc = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-000000000032"));
        var dest = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-000000000033"));
        var product = ProductId.From(Guid.Parse("00000000-0000-4000-8000-000000000034"));
        var routeId = FreightRouteId.From(Guid.Parse("00000000-0000-4000-8000-000000000035"));
        var route = new FreightRoute(routeId, loc, dest, TransitHours: 1, Capacity: Quantity.From(100m));
        var inventory = new InventoryStore();
        var shipment = new ActiveShipment(
            ShipmentId.From(Guid.Parse("00000000-0000-4000-8000-000000000036")),
            firm, routeId, product, Quantity.From(2m), Money.From(1m), 1, Now);

        var result = LogisticsEngine.AdvanceHour(
            new List<ActiveShipment> { shipment },
            inventory,
            new Dictionary<FreightRouteId, FreightRoute> { [routeId] = route });

        await Assert.That(result.Delivered.Count).IsEqualTo(1);
        await Assert.That(inventory.GetQuantity(new InventoryKey(firm, dest, product)).Value).IsEqualTo(2m);
    }

    private static (
        InventoryStore Inventory,
        FirmId Firm,
        TransportHub Hub,
        ProductId Product,
        ProductId Fuel,
        VehicleClass Vehicle,
        IReadOnlyDictionary<TransportCorridorId, TransportCorridor> Corridors,
        Itinerary Itinerary) BuildItineraryWorld()
    {
        var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000041"));
        var loc = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-000000000042"));
        var destHubId = TransportHubId.From(Guid.Parse("00000000-0000-4000-8000-000000000043"));
        var hub = new TransportHub(
            TransportHubId.From(Guid.Parse("00000000-0000-4000-8000-000000000044")),
            loc, "Origin", DwellHours: 0, BerthCapacity: 2);
        var product = ProductId.From(Guid.Parse("00000000-0000-4000-8000-000000000045"));
        var fuel = ProductId.From(Guid.Parse("00000000-0000-4000-8000-000000000046"));
        var vehicle = new VehicleClass(
            VehicleClassId.From(Guid.Parse("00000000-0000-4000-8000-000000000047")),
            CargoCapacity: Quantity.From(10m),
            FuelBurnPerDifficultyHour: 2m,
            CrewLaborPerUnderwayHour: 1m,
            FuelTankCapacity: Quantity.From(8m));
        var corridor = new TransportCorridor(
            TransportCorridorId.From(Guid.Parse("00000000-0000-4000-8000-000000000048")),
            hub.Id, destHubId,
            TransitHours: 2, MaxCargo: Quantity.From(10m), Difficulty: 1m, Toll: Money.From(1m));
        var corridors = new Dictionary<TransportCorridorId, TransportCorridor> { [corridor.Id] = corridor };
        ItineraryPlanner.TryPlan(hub.Id, destHubId, vehicle.CargoCapacity, vehicle, corridors, out var itinerary);
        return (new InventoryStore(), firm, hub, product, fuel, vehicle, corridors, itinerary);
    }
}
