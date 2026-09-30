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
