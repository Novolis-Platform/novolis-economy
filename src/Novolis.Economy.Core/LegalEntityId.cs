namespace Novolis.Economy.Core;

/// <summary>Strong id for a legal entity.</summary>
public readonly record struct LegalEntityId(Guid Value)
{
    public static LegalEntityId From(Guid value) => new(value);
    public static LegalEntityId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}
