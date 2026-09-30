using System.Collections.Immutable;
using Novolis.Economy;

namespace Novolis.Economy.Logistics;

/// <summary>Variable haul cost for a planned itinerary (fuel + tolls + crew wages).</summary>
/// <param name="UnderwayHours">Sum of profile-scaled corridor transit hours.</param>
/// <param name="FuelUnits">Fuel burned along the path.</param>
/// <param name="Tolls">Sum of corridor tolls.</param>
/// <param name="CrewHours">Crew labor hours (underway × crew rate).</param>
/// <param name="FuelCost">FuelUnits × fuel unit cost.</param>
/// <param name="CrewCost">CrewHours × wage rate.</param>
/// <param name="TotalVariableCost">Fuel + tolls + crew.</param>
/// <param name="DriveWear">Estimated drive wear for the itinerary.</param>
public readonly record struct HaulCostEstimate(
  long UnderwayHours,
  decimal FuelUnits,
  Money Tolls,
  decimal CrewHours,
  Money FuelCost,
  Money CrewCost,
  Money TotalVariableCost,
  decimal DriveWear = 0m);
