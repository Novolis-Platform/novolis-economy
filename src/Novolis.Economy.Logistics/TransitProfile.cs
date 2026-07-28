using Novolis.Economy;

namespace Novolis.Economy.Logistics;

/// <summary>
/// Operating speed / cost / wear choice for an FTL (or soft-SF) leg.
/// Corridor tables store a StandardCommercial baseline; profiles scale hours, fuel, and drive wear.
/// </summary>
/// <remarks>
/// Policy fiction: mass and urgency choose the profile — bulk wants Slow, high time-value wants Priority.
/// Faster is not free: fuel and drive wear rise faster than hours fall.
/// </remarks>
public enum TransitProfile : byte
{
  /// <summary>Bulk / automated haulers — minimize cost and wear; patience required.</summary>
  SlowEconomic = 0,

  /// <summary>Default crewed commercial balance of schedule, fuel, and insurance.</summary>
  StandardCommercial = 1,

  /// <summary>High time-value cargo — fewer hours, more fuel and drive stress.</summary>
  PriorityCommercial = 2,
}

/// <summary>Multipliers applied to corridor baseline hours and fuel burn.</summary>
/// <param name="HoursFactor">Scales underway hours (and planner path cost).</param>
/// <param name="FuelFactor">Scales fuel burn for the same corridor.</param>
/// <param name="WearPerUnderwayHour">Drive wear units accrued per underway hour at this profile.</param>
public readonly record struct TransitProfileFactors(
  decimal HoursFactor,
  decimal FuelFactor,
  decimal WearPerUnderwayHour);

/// <summary>Lookup table for <see cref="TransitProfile"/> factors.</summary>
public static class TransitProfiles
{
  /// <summary>Factors for each profile (tunable; keep Slow cheaper and Priority costlier).</summary>
  public static TransitProfileFactors Factors(TransitProfile profile) =>
    profile switch
    {
      TransitProfile.SlowEconomic => new(HoursFactor: 1.55m, FuelFactor: 0.72m, WearPerUnderwayHour: 0.45m),
      TransitProfile.PriorityCommercial => new(HoursFactor: 0.62m, FuelFactor: 1.85m, WearPerUnderwayHour: 2.40m),
      _ => new(HoursFactor: 1.00m, FuelFactor: 1.00m, WearPerUnderwayHour: 1.00m),
    };

  /// <summary>Clamp unknown codes to StandardCommercial.</summary>
  public static TransitProfile FromCode(int code) =>
    code switch
    {
      (int)TransitProfile.SlowEconomic => TransitProfile.SlowEconomic,
      (int)TransitProfile.PriorityCommercial => TransitProfile.PriorityCommercial,
      _ => TransitProfile.StandardCommercial,
    };

  /// <summary>Scaled transit hours for a corridor under a profile (minimum 1).</summary>
  public static long EffectiveHours(TransportCorridor corridor, TransitProfile profile)
  {
    var f = Factors(profile).HoursFactor;
    var raw = Math.Max(1m, corridor.TransitHours) * f;
    return Math.Max(1L, (long)Math.Ceiling((double)raw));
  }

  /// <summary>
  /// Drive life consumed per underway hour: profile wear × cargo mass load × lane difficulty.
  /// Empty hulls are gentler; Priority + full holds burn life faster. Guarantees eventual overhaul need.
  /// </summary>
  /// <param name="profile">Operating profile.</param>
  /// <param name="cargoLoadFraction">Cargo / capacity (0 empty … 1 full; may exceed 1 slightly).</param>
  /// <param name="difficulty">Corridor difficulty (≥1).</param>
  public static decimal WearForUnderwayHour(
    TransitProfile profile,
    decimal cargoLoadFraction,
    decimal difficulty = 1m)
  {
    var baseWear = Factors(profile).WearPerUnderwayHour;
    var load = Math.Clamp(cargoLoadFraction, 0m, 1.5m);
    // Empty ≈ 0.72×, full ≈ 1.27× — mass on the drive field.
    var massFactor = 0.72m + 0.55m * load;
    var diff = Math.Max(1m, difficulty);
    var difficultyFactor = 0.85m + 0.15m * Math.Min(diff, 4m);
    return baseWear * massFactor * difficultyFactor;
  }
}
