namespace Novolis.Economy;

/// <summary>Ops firm key. Same Guid space as Core <c>LegalEntityId</c>.</summary>
public readonly record struct FirmId(Guid Value)
{
  public static FirmId New() => new(Guid.NewGuid());
  public static FirmId From(Guid value) => new(value);
  public Core.LegalEntityId AsCore() => Core.LegalEntityId.From(Value);
  public static FirmId From(Core.LegalEntityId id) => new(id.Value);
  public override string ToString() => Value.ToString("N");
}

/// <summary>Ops product key. Same Guid space as Core <c>ResourceId</c>.</summary>
public readonly record struct ProductId(Guid Value)
{
  public static ProductId New() => new(Guid.NewGuid());
  public static ProductId From(Guid value) => new(value);
  public Core.ResourceId AsCore() => Core.ResourceId.From(Value);
  public static ProductId From(Core.ResourceId id) => new(id.Value);
  public override string ToString() => Value.ToString("N");
}

/// <summary>Ops area key. Same Guid space as Core <c>RegionId</c>.</summary>
public readonly record struct GeographicAreaId(Guid Value)
{
  public static GeographicAreaId New() => new(Guid.NewGuid());
  public static GeographicAreaId From(Guid value) => new(value);
  public Core.RegionId AsCore() => Core.RegionId.From(Value);
  public static GeographicAreaId From(Core.RegionId id) => new(id.Value);
  public override string ToString() => Value.ToString("N");
}

/// <summary>Ops cohort key. Same Guid space as Core <c>CohortId</c>.</summary>
public readonly record struct ConsumerCohortId(Guid Value)
{
  public static ConsumerCohortId New() => new(Guid.NewGuid());
  public static ConsumerCohortId From(Guid value) => new(value);
  public Core.CohortId AsCore() => Core.CohortId.From(Value);
  public static ConsumerCohortId From(Core.CohortId id) => new(id.Value);
  public override string ToString() => Value.ToString("N");
}
