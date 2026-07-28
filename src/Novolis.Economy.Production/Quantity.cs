namespace Novolis.Economy;

/// <summary>Physical or countable quantity using decimal arithmetic.</summary>
/// <param name="Value">Quantity in product-specific units.</param>
public readonly record struct Quantity(decimal Value) : IComparable<Quantity>
{
  public static Quantity Zero { get; } = new(0m);
  public static Quantity From(decimal value) => new(value);
  public int CompareTo(Quantity other) => Value.CompareTo(other.Value);
  public static Quantity operator +(Quantity left, Quantity right) => new(left.Value + right.Value);
  public static Quantity operator -(Quantity left, Quantity right) => new(left.Value - right.Value);
  public static Quantity operator *(Quantity left, decimal scalar) => new(left.Value * scalar);
  public static bool operator <(Quantity left, Quantity right) => left.Value < right.Value;
  public static bool operator >(Quantity left, Quantity right) => left.Value > right.Value;
  public static bool operator <=(Quantity left, Quantity right) => left.Value <= right.Value;
  public static bool operator >=(Quantity left, Quantity right) => left.Value >= right.Value;
  public override string ToString() => Value.ToString("0.####");
}

/// <summary>Percentage value where 100 represents 100%.</summary>
public readonly record struct Percentage(decimal Value)
{
  public static Percentage Zero { get; } = new(0m);
  public static Percentage FromPoints(decimal points) => new(points);
  public static Percentage FromFraction(decimal fraction) => new(fraction * 100m);
  public decimal AsFraction => Value / 100m;
  public override string ToString() => $"{Value:0.####}%";
}
