namespace Novolis.Economy.Core;

/// <summary>Strong id for a payment obligation.</summary>
public readonly record struct ObligationId(Guid Value)
{
  public static ObligationId From(Guid value) => new(value);
  public static ObligationId New() => new(Guid.NewGuid());
  public override string ToString() => Value.ToString("N");
}