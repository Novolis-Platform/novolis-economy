using Novolis.Economy;

namespace Novolis.Economy.Logistics;

/// <summary>Multipliers applied to corridor baseline hours and fuel burn.</summary>
/// <param name="HoursFactor">Scales underway hours (and planner path cost).</param>
/// <param name="FuelFactor">Scales fuel burn for the same corridor.</param>
/// <param name="WearPerUnderwayHour">Drive wear units accrued per underway hour at this profile.</param>
public readonly record struct TransitProfileFactors(
  decimal HoursFactor,
  decimal FuelFactor,
  decimal WearPerUnderwayHour);
