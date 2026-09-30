namespace Novolis.Economy;

public readonly record struct ProductionProcessId(Guid Value)
{
  public static ProductionProcessId New() => new(Guid.NewGuid());
  public static ProductionProcessId From(Guid value) => new(value);
  public override string ToString() => Value.ToString("N");
}
