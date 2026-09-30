namespace Novolis.Economy;

/// <summary>Ops area key. Same Guid space as Core <c>RegionId</c>.</summary>
public readonly record struct GeographicAreaId(Guid Value)
{
  public static GeographicAreaId New() => new(Guid.NewGuid());
  public static GeographicAreaId From(Guid value) => new(value);
  public Primitives.RegionId AsCore() => Primitives.RegionId.From(Value);
  public static GeographicAreaId From(Primitives.RegionId id) => new(id.Value);
  public override string ToString() => Value.ToString("N");
}
