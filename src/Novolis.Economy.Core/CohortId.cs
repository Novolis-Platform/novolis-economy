namespace Novolis.Economy.Core;

/// <summary>Strong id for a household cohort.</summary>
public readonly record struct CohortId(Guid Value)
{
  public static CohortId From(Guid value) => new(value);
  public static CohortId New() => new(Guid.NewGuid());
  public override string ToString() => Value.ToString("N");
}