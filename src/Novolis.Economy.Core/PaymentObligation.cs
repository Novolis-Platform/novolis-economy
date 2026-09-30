namespace Novolis.Economy.Core;

/// <summary>Timed payment claim (SPEC §13).</summary>
public sealed record PaymentObligation(
  ObligationId Id,
  LegalEntityId Debtor,
  LegalEntityId Creditor,
  Money Amount,
  int DuePeriod,
  ObligationKind Kind,
  ObligationStatus Status);