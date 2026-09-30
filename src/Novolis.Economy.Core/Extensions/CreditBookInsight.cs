namespace Novolis.Economy.Core.Extensions;

/// <summary>Credit facilities and loan book.</summary>
public sealed record CreditBookInsight(
  int FacilityCount,
  Money FacilityLimitTotal,
  Money FacilityDrawnTotal,
  Money UndrawnCommitted,
  int PerformingLoans,
  int DelinquentLoans,
  int DefaultedLoans,
  Money LoanPrincipalOutstanding);