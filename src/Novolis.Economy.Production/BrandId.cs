namespace Novolis.Economy;

public readonly record struct BrandId(Guid Value)
{
  public static BrandId New() => new(Guid.NewGuid());
  public override string ToString() => Value.ToString("N");
}
