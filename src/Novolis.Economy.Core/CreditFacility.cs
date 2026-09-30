namespace Novolis.Economy.Core;

/// <summary>Capacity to create debt (SPEC §12).</summary>
public sealed record CreditFacility(
  CreditFacilityId Id,
  LegalEntityId Provider,
  LegalEntityId Borrower,
  Money Limit,
  Money Drawn,
  bool IsCommitted)
{
  /// <summary>Undrawn capacity.</summary>
  public Money Available => Money.From(Math.Max(0m, Limit.Amount - Drawn.Amount));
}