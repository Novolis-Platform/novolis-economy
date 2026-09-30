namespace Novolis.Economy.Core;

/// <summary>Lifecycle of an outstanding loan (SPEC §11).</summary>
public enum LoanStatus
{
  Performing = 0,
  Delinquent,
  Defaulted,
  Repaid
}