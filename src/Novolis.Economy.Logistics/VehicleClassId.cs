namespace Novolis.Economy;

public readonly record struct VehicleClassId(Guid Value)
{
  public static VehicleClassId New() => new(Guid.NewGuid());
  public static VehicleClassId From(Guid value) => new(value);
  public override string ToString() => Value.ToString("N");
}
