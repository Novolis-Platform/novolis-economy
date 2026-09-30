namespace Novolis.Economy.Primitives;

/// <summary>Identity of an economic financial claim.</summary>
public readonly record struct ClaimId(Guid Value)
{
    public static ClaimId From(Guid value) => new(value);
    public static ClaimId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}
