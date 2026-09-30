namespace Novolis.Economy.Core;

/// <summary>Existing debt instrument (SPEC §11).</summary>
public sealed record Loan(
  LoanId Id,
  LegalEntityId Lender,
  LegalEntityId Borrower,
  Money PrincipalOutstanding,
  decimal InterestRatePerPeriod,
  int RemainingPeriods,
  LoanStatus Status);