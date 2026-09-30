namespace Novolis.Economy.Core;

/// <summary>Payment obligation status (SPEC §13).</summary>
public enum ObligationStatus
{
  Pending = 0,
  Paid,
  Delinquent,
  Defaulted
}