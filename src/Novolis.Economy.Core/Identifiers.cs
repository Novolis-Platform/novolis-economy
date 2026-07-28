namespace Novolis.Economy.Core;

/// <summary>Strong id for a legal entity.</summary>
public readonly record struct LegalEntityId(Guid Value)
{
    public static LegalEntityId From(Guid value) => new(value);
    public static LegalEntityId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}

/// <summary>Strong id for a region.</summary>
public readonly record struct RegionId(Guid Value)
{
    public static RegionId From(Guid value) => new(value);
    public static RegionId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}

/// <summary>Strong id for a household cohort.</summary>
public readonly record struct CohortId(Guid Value)
{
    public static CohortId From(Guid value) => new(value);
    public static CohortId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}

/// <summary>Strong id for an activity.</summary>
public readonly record struct ActivityId(Guid Value)
{
    public static ActivityId From(Guid value) => new(value);
    public static ActivityId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}

/// <summary>Strong id for a resource type.</summary>
public readonly record struct ResourceId(Guid Value)
{
    public static ResourceId From(Guid value) => new(value);
    public static ResourceId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}

/// <summary>Strong id for a loan.</summary>
public readonly record struct LoanId(Guid Value)
{
    public static LoanId From(Guid value) => new(value);
    public static LoanId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}

/// <summary>Strong id for a credit facility.</summary>
public readonly record struct CreditFacilityId(Guid Value)
{
    public static CreditFacilityId From(Guid value) => new(value);
    public static CreditFacilityId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}

/// <summary>Strong id for a payment obligation.</summary>
public readonly record struct ObligationId(Guid Value)
{
    public static ObligationId From(Guid value) => new(value);
    public static ObligationId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}
