namespace Novolis.Economy.Primitives;

/// <summary>Identity of an owned or traded economic asset.</summary>
public readonly record struct EconomicAssetId(Guid Value)
{
    public static EconomicAssetId From(Guid value) => new(value);
    public static EconomicAssetId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}
