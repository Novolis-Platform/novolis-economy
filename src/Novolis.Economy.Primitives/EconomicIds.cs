namespace Novolis.Economy.Primitives;

/// <summary>Identity of an economic owner.</summary>
public readonly record struct EconomicEntityId(Guid Value)
{
    public static EconomicEntityId From(Guid value) => new(value);
    public static EconomicEntityId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}

/// <summary>Identity of an owned or traded economic asset.</summary>
public readonly record struct EconomicAssetId(Guid Value)
{
    public static EconomicAssetId From(Guid value) => new(value);
    public static EconomicAssetId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}

/// <summary>Identity of a spatial region.</summary>
public readonly record struct RegionId(Guid Value)
{
    public static RegionId From(Guid value) => new(value);
    public static RegionId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}

/// <summary>Identity of an economic financial claim.</summary>
public readonly record struct ClaimId(Guid Value)
{
    public static ClaimId From(Guid value) => new(value);
    public static ClaimId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}

/// <summary>Identity of an atomic economic transaction.</summary>
public readonly record struct TransactionId(Guid Value)
{
    public static TransactionId From(Guid value) => new(value);
    public static TransactionId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}
