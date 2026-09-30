namespace Novolis.Economy;

/// <summary>Ops cohort key. Same Guid space as Core <c>CohortId</c>.</summary>
public readonly record struct ConsumerCohortId(Guid Value)
{
  public static ConsumerCohortId New() => new(Guid.NewGuid());
  public static ConsumerCohortId From(Guid value) => new(value);
  public Core.CohortId AsCore() => Core.CohortId.From(Value);
  public static ConsumerCohortId From(Core.CohortId id) => new(id.Value);
  public override string ToString() => Value.ToString("N");
}
