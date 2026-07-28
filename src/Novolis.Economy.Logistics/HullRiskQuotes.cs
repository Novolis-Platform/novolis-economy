namespace Novolis.Economy.Logistics;

/// <summary>
/// Pure hull insurance / overhaul quote math (risk pricing, not a terminal tax).
/// Hosts supply base premium and exposure counters from their registry entries.
/// </summary>
public static class HullRiskQuotes
{
  /// <summary>
  /// Daily insurance: base × life-fraction risk × modest profile exposure × actuarial.
  /// Idle / suspended / burned-out hulls pay a small standing fee only.
  /// </summary>
  public static decimal DailyPremium(
    decimal basePremium,
    decimal lifeFraction,
    int priorityLegs,
    int longLaneLegs,
    decimal actuarialLoad,
    bool idleOrSuspended)
  {
    if (idleOrSuspended)
    {
      return Math.Round(basePremium * 0.25m * actuarialLoad, 2, MidpointRounding.AwayFromZero);
    }

    var lifeRisk = 1m + 0.75m * Math.Min(1m, lifeFraction);
    var priorityFactor = 1m + Math.Min(0.35m, priorityLegs * 0.008m);
    var longFactor = 1m + Math.Min(0.25m, longLaneLegs * 0.006m);
    var quote = basePremium * lifeRisk * priorityFactor * longFactor * actuarialLoad;
    var cap = basePremium * 2.8m * actuarialLoad;
    return Math.Round(Math.Min(quote, cap), 2, MidpointRounding.AwayFromZero);
  }

  /// <summary>Elective overhaul (before burnout) — cheaper scheduled stack swap.</summary>
  public static decimal ElectiveOverhaul(decimal lifeUsed, decimal hullScale = 1m) =>
    Math.Round((520m + lifeUsed * 0.04m) * hullScale, 2, MidpointRounding.AwayFromZero);

  /// <summary>Forced overhaul after guaranteed burnout — yard emergency.</summary>
  public static decimal BurnoutOverhaul(decimal lifeUsed, decimal hullScale = 1m) =>
    Math.Round(ElectiveOverhaul(lifeUsed, hullScale) * 2.15m, 2, MidpointRounding.AwayFromZero);
}
