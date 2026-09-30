using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Repay principal (and accrued interest) on a loan up to the given amount.</summary>
public sealed record RepayLoan(
  LoanId LoanId,
  Money Amount) : IEconomyCommand;
