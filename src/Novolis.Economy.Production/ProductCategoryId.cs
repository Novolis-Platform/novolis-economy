namespace Novolis.Economy;

public readonly record struct ProductCategoryId(Guid Value)
{
  public static ProductCategoryId New() => new(Guid.NewGuid());
  public static ProductCategoryId From(Guid value) => new(value);
  public override string ToString() => Value.ToString("N");
}
