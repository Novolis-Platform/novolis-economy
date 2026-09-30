using System.Collections.Immutable;
using Novolis.Economy;

namespace Novolis.Economy.Logistics;

/// <summary>Thin vehicle capability profile.</summary>
/// <param name="Id">Class id.</param>
/// <param name="CargoCapacity">Max cargo quantity.</param>
/// <param name="FuelBurnPerDifficultyHour">Fuel units burned per (transitHour * difficulty).</param>
/// <param name="CrewLaborPerUnderwayHour">Labor hours accrued per underway hour.</param>
/// <param name="FuelTankCapacity">Max onboard fuel.</param>
public sealed record VehicleClass(
  VehicleClassId Id,
  Quantity CargoCapacity,
  decimal FuelBurnPerDifficultyHour,
  decimal CrewLaborPerUnderwayHour,
  Quantity FuelTankCapacity);
