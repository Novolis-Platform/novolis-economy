namespace Novolis.Economy.Primitives;

/// <summary>Identity of an atomic economic transaction.</summary>
public readonly record struct TransactionId(Guid Value)
{
    public static TransactionId From(Guid value) => new(value);
    public static TransactionId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}
