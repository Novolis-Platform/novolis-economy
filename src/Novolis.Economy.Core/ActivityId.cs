namespace Novolis.Economy.Core;

/// <summary>Strong id for an activity.</summary>
public readonly record struct ActivityId(Guid Value)
{
  public static ActivityId From(Guid value) => new(value);
  public static ActivityId New() => new(Guid.NewGuid());
  public override string ToString() => Value.ToString("N");
}