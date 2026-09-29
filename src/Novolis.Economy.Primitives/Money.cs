using System.Text.Json.Serialization;

namespace Novolis.Economy.Primitives;

/// <summary>Monetary amount in the scenario's unit of account.</summary>
public readonly record struct Money : IComparable<Money>
{
    [JsonConstructor]
    public Money(decimal amount) => Amount = amount;

    public decimal Amount { get; }

    public static Money Zero { get; } = new(0m);
    public static Money From(decimal amount) => new(amount);

    public int CompareTo(Money other) => Amount.CompareTo(other.Amount);

    public static Money operator +(Money left, Money right) => new(left.Amount + right.Amount);
    public static Money operator -(Money left, Money right) => new(left.Amount - right.Amount);
    public static Money operator *(Money left, decimal scalar) => new(left.Amount * scalar);
    public static bool operator <(Money left, Money right) => left.Amount < right.Amount;
    public static bool operator >(Money left, Money right) => left.Amount > right.Amount;
    public static bool operator <=(Money left, Money right) => left.Amount <= right.Amount;
    public static bool operator >=(Money left, Money right) => left.Amount >= right.Amount;

    public override string ToString() => Amount.ToString("0.####");
}
