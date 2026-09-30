namespace Novolis.Economy.Core;

/// <summary>Strong id for a resource type.</summary>
public readonly record struct ResourceId(Guid Value)
{
  public static ResourceId From(Guid value) => new(value);
  public static ResourceId New() => new(Guid.NewGuid());
  public override string ToString() => Value.ToString("N");
}