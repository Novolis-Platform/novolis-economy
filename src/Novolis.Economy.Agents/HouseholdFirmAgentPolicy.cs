using Novolis.Economy;

namespace Novolis.Economy.Agents;

/// <summary>Comfort invest / lend policy for a household cohort firm.</summary>
public sealed record HouseholdFirmAgentPolicy(
  FirmId? PreferredBorrower = null,
  FirmId? PreferredIssuer = null,
  Money? LoanPrincipal = null,
  decimal AnnualInterestRate = 0.08m,
  long TermHours = 72,
  decimal PurchaseFraction = 0.01m,
  Money? PurchasePrice = null,
  int MaxActiveLoans = 1);
