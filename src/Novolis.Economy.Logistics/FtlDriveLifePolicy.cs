namespace Novolis.Economy.Logistics;

/// <summary>
/// FTL drive life / overhaul constants shared by commercial registry hosts.
/// Hosts map hull class → rated life and fold underway mileage into life used.
/// </summary>
public static class FtlDriveLifePolicy
{
  /// <summary>Rated life before guaranteed burnout for light / tramp hulls.</summary>
  public const decimal RatedLifeLight = 9_000m;

  /// <summary>Rated life before guaranteed burnout for mega / bulk hulls.</summary>
  public const decimal RatedLifeMega = 22_000m;

  /// <summary>Elective overhaul window opens here; waiting past rated life guarantees burnout.</summary>
  public const decimal ElectiveOverhaulFraction = 0.72m;

  /// <summary>Daily decay applied to acute drive wear (claims / soft stress).</summary>
  public const decimal AcuteWearDecayPerDay = 0.04m;

  /// <summary>Missed premium days before a host registry may mark uninsured (late-payment spiral).</summary>
  public const int PremiumGraceDays = 14;

  /// <summary>Picks rated life from a hull-class label (Mega → mega band).</summary>
  public static decimal RatedLifeForHull(string hullClass) =>
    hullClass.Contains("Mega", StringComparison.OrdinalIgnoreCase)
      ? RatedLifeMega
      : RatedLifeLight;
}
