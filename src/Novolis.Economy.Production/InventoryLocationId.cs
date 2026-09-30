namespace Novolis.Economy;

public readonly record struct InventoryLocationId(Guid Value)
{
  public static InventoryLocationId New() => new(Guid.NewGuid());
  public static InventoryLocationId From(Guid value) => new(value);
  public override string ToString() => Value.ToString("N");
}
