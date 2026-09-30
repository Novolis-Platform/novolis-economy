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
