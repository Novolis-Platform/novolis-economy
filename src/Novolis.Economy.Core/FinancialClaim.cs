namespace Novolis.Economy.Core;

/// <summary>
/// Authoritative economic claim. Finance owns how a loan behaves; Core owns
/// who is owed the outstanding quantity and who owes it.
/// </summary>
public sealed record FinancialClaim(
  ClaimId Id,
  EconomicEntityId Creditor,
  EconomicEntityId Debtor,
  AssetAmount Principal,
  decimal InterestRatePerPeriod,
  int RemainingPeriods,
  LoanStatus Status,
  decimal AccruedInterest = 0m,
  AssetAmount? OriginalPrincipal = null);