namespace Novolis.Economy;

/// <summary>Ops product key. Same Guid space as Core <c>ResourceId</c>.</summary>
public readonly record struct ProductId(Guid Value)
{
  public static ProductId New() => new(Guid.NewGuid());
  public static ProductId From(Guid value) => new(value);
  public Core.ResourceId AsCore() => Core.ResourceId.From(Value);
  public static ProductId From(Core.ResourceId id) => new(id.Value);
  public override string ToString() => Value.ToString("N");
}
