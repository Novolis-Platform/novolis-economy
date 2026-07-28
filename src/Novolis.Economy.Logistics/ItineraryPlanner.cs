using System.Collections.Immutable;
using Novolis.Economy;

namespace Novolis.Economy.Logistics;

/// <summary>Finds a feasible corridor path between hubs (Dijkstra on profile-scaled transit hours).</summary>
public static class ItineraryPlanner
{
  /// <summary>
  /// Plans an itinerary from <paramref name="origin"/> to <paramref name="destination"/>.
  /// Considers cargo capacity and optional fuel range (max burn that can be bunkered per hub stop).
  /// </summary>
  public static bool TryPlan(
    TransportHubId origin,
    TransportHubId destination,
    Quantity cargoQuantity,
    VehicleClass vehicle,
    IReadOnlyDictionary<TransportCorridorId, TransportCorridor> corridors,
    out Itinerary itinerary,
    TransitProfile profile = TransitProfile.StandardCommercial)
  {
    itinerary = Itinerary.Empty;
    if (origin.Equals(destination))
    {
      return false;
    }

    if (cargoQuantity.Value > vehicle.CargoCapacity.Value)
    {
      return false;
    }

    var outgoing = corridors.Values
      .Where(c => c.MaxCargo.Value >= cargoQuantity.Value)
      .GroupBy(c => c.From)
      .ToDictionary(g => g.Key, g => g.ToList());

    var dist = new Dictionary<TransportHubId, long> { [origin] = 0 };
    var prev = new Dictionary<TransportHubId, (TransportHubId From, TransportCorridorId Corridor)>();
    var open = new PriorityQueue<TransportHubId, long>();
    open.Enqueue(origin, 0);

    while (open.TryDequeue(out var hub, out var cost))
    {
      if (cost > dist.GetValueOrDefault(hub, long.MaxValue))
      {
        continue;
      }

      if (hub.Equals(destination))
      {
        break;
      }

      if (!outgoing.TryGetValue(hub, out var edges))
      {
        continue;
      }

      foreach (var edge in edges.OrderBy(e => e.Id.Value))
      {
        var burn = FuelBurnForLeg(edge, vehicle, profile);
        if (burn.Value > vehicle.FuelTankCapacity.Value)
        {
          // Cannot traverse even with a full tank (range scarcity).
          continue;
        }

        var nextCost = cost + TransitProfiles.EffectiveHours(edge, profile);
        var known = dist.GetValueOrDefault(edge.To, long.MaxValue);
        if (nextCost >= known)
        {
          continue;
        }

        dist[edge.To] = nextCost;
        prev[edge.To] = (hub, edge.Id);
        open.Enqueue(edge.To, nextCost);
      }
    }

    if (!prev.ContainsKey(destination) && !origin.Equals(destination))
    {
      if (!dist.ContainsKey(destination))
      {
        return false;
      }
    }

    if (!dist.ContainsKey(destination))
    {
      return false;
    }

    var stack = new Stack<TransportCorridorId>();
    var cursor = destination;
    while (!cursor.Equals(origin))
    {
      if (!prev.TryGetValue(cursor, out var step))
      {
        return false;
      }

      stack.Push(step.Corridor);
      cursor = step.From;
    }

    itinerary = new Itinerary(stack.ToImmutableArray());
    return itinerary.LegCount > 0;
  }

  /// <summary>Fuel quantity required to traverse a corridor under a transit profile.</summary>
  public static Quantity FuelBurnForLeg(
    TransportCorridor corridor,
    VehicleClass vehicle,
    TransitProfile profile = TransitProfile.StandardCommercial)
  {
    var hours = Math.Max(1m, corridor.TransitHours);
    var difficulty = corridor.Difficulty <= 0m ? 1m : corridor.Difficulty;
    var factors = TransitProfiles.Factors(profile);
    var burn = hours * difficulty * vehicle.FuelBurnPerDifficultyHour * factors.FuelFactor;
    return Quantity.From(Math.Round(burn, 6, MidpointRounding.AwayFromZero));
  }
}
