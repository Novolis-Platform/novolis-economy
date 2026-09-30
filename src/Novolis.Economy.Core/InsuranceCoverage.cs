namespace Novolis.Economy.Core;

/// <summary>Insurance contract (SPEC §16).</summary>
public sealed record InsuranceCoverage(
  LegalEntityId Insurer,
  LegalEntityId Insured,
  RiskKind Risk,
  decimal CoveredFraction,
  Money Deductible,
  Money PremiumPerPeriod);