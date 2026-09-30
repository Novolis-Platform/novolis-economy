namespace Novolis.Economy;

public readonly record struct OperatingUnitId(Guid Value)
{
  public static OperatingUnitId New() => new(Guid.NewGuid());
  public static OperatingUnitId From(Guid value) => new(value);
  public override string ToString() => Value.ToString("N");
}
