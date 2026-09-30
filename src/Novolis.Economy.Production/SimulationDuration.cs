namespace Novolis.Economy;

/// <summary>Duration expressed in simulation hours.</summary>
/// <param name="Hours">Number of hours to advance.</param>
public readonly record struct SimulationDuration(long Hours)
{
  /// <summary>One simulation hour.</summary>
  public static SimulationDuration OneHour { get; } = new(1);

  /// <summary>One simulation day (24 hours).</summary>
  public static SimulationDuration OneDay { get; } = new(SimulationHour.HoursPerDay);

  /// <summary>Creates a duration from hours; must be non-negative.</summary>
  public static SimulationDuration FromHours(long hours)
  {
    ArgumentOutOfRangeException.ThrowIfNegative(hours);
    return new(hours);
  }
}
