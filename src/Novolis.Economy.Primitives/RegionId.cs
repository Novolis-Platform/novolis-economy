namespace Novolis.Economy.Primitives;

/// <summary>Identity of a spatial region.</summary>
public readonly record struct RegionId(Guid Value)
{
    public static RegionId From(Guid value) => new(value);
    public static RegionId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}
