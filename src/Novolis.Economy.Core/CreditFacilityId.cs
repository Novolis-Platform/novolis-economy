namespace Novolis.Economy.Core;

/// <summary>Strong id for a credit facility.</summary>
public readonly record struct CreditFacilityId(Guid Value)
{
  public static CreditFacilityId From(Guid value) => new(value);
  public static CreditFacilityId New() => new(Guid.NewGuid());
  public override string ToString() => Value.ToString("N");
}