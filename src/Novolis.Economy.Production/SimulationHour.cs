namespace Novolis.Economy;

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
