namespace Novolis.Economy.Core;

/// <summary>Strong id for a loan.</summary>
public readonly record struct LoanId(Guid Value)
{
  public static LoanId From(Guid value) => new(value);
  public static LoanId New() => new(Guid.NewGuid());
  public override string ToString() => Value.ToString("N");
}