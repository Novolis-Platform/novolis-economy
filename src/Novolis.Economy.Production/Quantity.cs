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
