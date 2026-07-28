namespace Novolis.Economy;

/// <summary>Monetary amount using decimal arithmetic.</summary>
/// <param name="Amount">Currency units (caller-defined scale).</param>
public readonly record struct Money(decimal Amount) : IComparable<Money>
{
  /// <summary>Zero money.</summary>
  public static Money Zero { get; } = new(0m);

  /// <summary>Creates money; rejects non-finite values via decimal constraints.</summary>
  public static Money From(decimal amount) => new(amount);

  /// <inheritdoc />
  public int CompareTo(Money other) => Amount.CompareTo(other.Amount);

  /// <summary>Adds two money values.</summary>
  public static Money operator +(Money left, Money right) => new(left.Amount + right.Amount);

  /// <summary>Subtracts money values.</summary>
  public static Money operator -(Money left, Money right) => new(left.Amount - right.Amount);

  /// <summary>Multiplies money by a scalar.</summary>
  public static Money operator *(Money left, decimal scalar) => new(left.Amount * scalar);

  /// <summary>Less-than comparison.</summary>
  public static bool operator <(Money left, Money right) => left.Amount < right.Amount;

  /// <summary>Greater-than comparison.</summary>
  public static bool operator >(Money left, Money right) => left.Amount > right.Amount;

  /// <summary>Less-or-equal comparison.</summary>
  public static bool operator <=(Money left, Money right) => left.Amount <= right.Amount;

  /// <summary>Greater-or-equal comparison.</summary>
  public static bool operator >=(Money left, Money right) => left.Amount >= right.Amount;

  /// <inheritdoc />
  public override string ToString() => Amount.ToString("0.####");
}

/// <summary>Physical or countable quantity using decimal arithmetic.</summary>
/// <param name="Value">Quantity in product-specific units.</param>
public readonly record struct Quantity(decimal Value) : IComparable<Quantity>
{
  /// <summary>Zero quantity.</summary>
  public static Quantity Zero { get; } = new(0m);

  /// <summary>Creates a quantity. Negative values are allowed only when explicitly modeling adjustments.</summary>
  public static Quantity From(decimal value) => new(value);

  /// <inheritdoc />
  public int CompareTo(Quantity other) => Value.CompareTo(other.Value);

  /// <summary>Adds quantities.</summary>
  public static Quantity operator +(Quantity left, Quantity right) => new(left.Value + right.Value);

  /// <summary>Subtracts quantities.</summary>
  public static Quantity operator -(Quantity left, Quantity right) => new(left.Value - right.Value);

  /// <summary>Multiplies quantity by a scalar.</summary>
  public static Quantity operator *(Quantity left, decimal scalar) => new(left.Value * scalar);

  /// <summary>Less-than comparison.</summary>
  public static bool operator <(Quantity left, Quantity right) => left.Value < right.Value;

  /// <summary>Greater-than comparison.</summary>
  public static bool operator >(Quantity left, Quantity right) => left.Value > right.Value;

  /// <summary>Less-or-equal comparison.</summary>
  public static bool operator <=(Quantity left, Quantity right) => left.Value <= right.Value;

  /// <summary>Greater-or-equal comparison.</summary>
  public static bool operator >=(Quantity left, Quantity right) => left.Value >= right.Value;

  /// <inheritdoc />
  public override string ToString() => Value.ToString("0.####");
}

/// <summary>Percentage value where 100 represents 100%.</summary>
/// <param name="Value">Percentage points (e.g. 18.4 means 18.4%).</param>
public readonly record struct Percentage(decimal Value)
{
  /// <summary>Zero percent.</summary>
  public static Percentage Zero { get; } = new(0m);

  /// <summary>Creates a percentage from percentage points.</summary>
  public static Percentage FromPoints(decimal points) => new(points);

  /// <summary>Creates a percentage from a 0–1 fraction.</summary>
  public static Percentage FromFraction(decimal fraction) => new(fraction * 100m);

  /// <summary>Fraction in 0–1 form.</summary>
  public decimal AsFraction => Value / 100m;

  /// <inheritdoc />
  public override string ToString() => $"{Value:0.####}%";
}
