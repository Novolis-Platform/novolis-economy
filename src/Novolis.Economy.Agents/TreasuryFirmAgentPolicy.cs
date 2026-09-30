using Novolis.Economy;

namespace Novolis.Economy.Agents;

/// <summary>Working-capital lending thresholds for a treasury firm.</summary>
public sealed record TreasuryFirmAgentPolicy(
  IReadOnlyList<FirmId> EligibleBorrowers,
  decimal CashFloorToLend,
  decimal BorrowerCashFloor,
  Money LoanPrincipal,
  decimal AnnualInterestRate,
  long TermHours,
  int MaxActiveLoansToBorrower = 1);
