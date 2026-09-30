namespace Novolis.Economy.Core;

/// <summary>Insured risk category (SPEC §16).</summary>
public enum RiskKind
{
  ProductionLoss = 0,
  TransportLoss,
  LiabilityLoss,
  CreditLoss
}