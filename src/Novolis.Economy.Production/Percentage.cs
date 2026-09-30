namespace Novolis.Economy;

/// <summary>Percentage value where 100 represents 100%.</summary>
public readonly record struct Percentage(decimal Value)
{
  public static Percentage Zero { get; } = new(0m);
  public static Percentage FromPoints(decimal points) => new(points);
  public static Percentage FromFraction(decimal fraction) => new(fraction * 100m);
  public decimal AsFraction => Value / 100m;
  public override string ToString() => $"{Value:0.####}%";
}
