namespace Novolis.Economy;

/// <summary>Calendar day in simulation time (day 0 is campaign start).</summary>
/// <param name="DayIndex">Zero-based day index.</param>
public readonly record struct SimulationDate(int DayIndex) : IComparable<SimulationDate>
{
  /// <summary>Campaign start date.</summary>
  public static SimulationDate Epoch { get; } = new(0);

  /// <inheritdoc />
  public int CompareTo(SimulationDate other) => DayIndex.CompareTo(other.DayIndex);

  /// <summary>Advances by the given number of days.</summary>
  public SimulationDate AddDays(int days) => new(checked(DayIndex + days));

  /// <inheritdoc />
  public override string ToString() => $"D{DayIndex}";
}

/// <summary>Absolute simulation hour (hour 0 is campaign start).</summary>
/// <param name="HourIndex">Zero-based hour index.</param>
public readonly record struct SimulationHour(long HourIndex) : IComparable<SimulationHour>
{
  /// <summary>Campaign start hour.</summary>
  public static SimulationHour Epoch { get; } = new(0);

  /// <summary>Hours per simulation day.</summary>
  public const int HoursPerDay = 24;

  /// <inheritdoc />
  public int CompareTo(SimulationHour other) => HourIndex.CompareTo(other.HourIndex);

  /// <summary>Date containing this hour.</summary>
  public SimulationDate Date => new((int)(HourIndex / HoursPerDay));

  /// <summary>Hour of day in 0..23.</summary>
  public int HourOfDay => (int)(HourIndex % HoursPerDay);

  /// <summary>Advances by hours.</summary>
  public SimulationHour AddHours(long hours) => new(checked(HourIndex + hours));

  /// <inheritdoc />
  public override string ToString() => $"H{HourIndex}";
}

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
