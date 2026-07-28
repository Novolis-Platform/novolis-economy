using System.Collections.Immutable;
using Novolis.Economy;

namespace Novolis.Economy.Logistics;

/// <summary>Variable haul cost for a planned itinerary (fuel + tolls + crew wages).</summary>
/// <param name="UnderwayHours">Sum of corridor transit hours.</param>
/// <param name="FuelUnits">Fuel burned along the path.</param>
/// <param name="Tolls">Sum of corridor tolls.</param>
/// <param name="CrewHours">Crew labor hours (underway × crew rate).</param>
/// <param name="FuelCost">FuelUnits × fuel unit cost.</param>
/// <param name="CrewCost">CrewHours × wage rate.</param>
/// <param name="TotalVariableCost">Fuel + tolls + crew.</param>
public readonly record struct HaulCostEstimate(
  long UnderwayHours,
  decimal FuelUnits,
  Money Tolls,
  decimal CrewHours,
  Money FuelCost,
  Money CrewCost,
  Money TotalVariableCost);

/// <summary>Pure haul cost estimator for multi-leg itineraries.</summary>
public static class HaulCostEstimator
{
  /// <summary>
  /// Estimates variable cost for an itinerary given corridor table, vehicle, and unit costs.
  /// Does not mutate world state.
  /// </summary>
  public static HaulCostEstimate Estimate(
    Itinerary itinerary,
    IReadOnlyDictionary<TransportCorridorId, TransportCorridor> corridors,
    VehicleClass vehicle,
    Money wageRatePerHour,
    Money fuelUnitCost)
  {
    long hours = 0;
    decimal fuel = 0m;
    var tolls = 0m;
    foreach (var legId in itinerary.CorridorIds)
    {
      if (!corridors.TryGetValue(legId, out var leg))
      {
        continue;
      }

      hours += Math.Max(1, leg.TransitHours);
      fuel += leg.TransitHours * leg.Difficulty * vehicle.FuelBurnPerDifficultyHour;
      tolls += leg.Toll.Amount;
    }

    var crewHours = hours * vehicle.CrewLaborPerUnderwayHour;
    var fuelCost = Money.From(fuel * fuelUnitCost.Amount);
    var crewCost = Money.From(crewHours * wageRatePerHour.Amount);
    var tollMoney = Money.From(tolls);
    return new HaulCostEstimate(
      hours,
      fuel,
      tollMoney,
      crewHours,
      fuelCost,
      crewCost,
      Money.From(fuelCost.Amount + tollMoney.Amount + crewCost.Amount));
  }

  /// <summary>Builds a one-leg itinerary from a corridor id.</summary>
  public static Itinerary SingleLeg(TransportCorridorId corridorId) =>
    new(ImmutableArray.Create(corridorId));
}
