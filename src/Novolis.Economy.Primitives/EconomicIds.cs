namespace Novolis.Economy.Primitives;

/// <summary>Identity of an economic owner.</summary>
public readonly record struct EconomicEntityId(Guid Value)
{
    public static EconomicEntityId From(Guid value) => new(value);
    public static EconomicEntityId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}
