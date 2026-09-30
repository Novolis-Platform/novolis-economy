namespace Novolis.Economy.Core;

/// <summary>Payment obligation kind (SPEC §13).</summary>
public enum ObligationKind
{
  Trade = 0,
  Wage,
  Tax,
  Interest,
  Principal,
  Dividend,
  InsurancePremium,
  InsuranceClaim
}