namespace Novolis.Economy;

public readonly record struct FacilityId(Guid Value)
{
  public static FacilityId New() => new(Guid.NewGuid());
  public static FacilityId From(Guid value) => new(value);
  public override string ToString() => Value.ToString("N");
}
