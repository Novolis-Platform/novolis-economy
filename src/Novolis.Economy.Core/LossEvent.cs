namespace Novolis.Economy.Core;

/// <summary>Pending loss events applied during the insurance step.</summary>
public sealed record LossEvent(
  LegalEntityId Insured,
  RiskKind Risk,
  Money GrossLoss);