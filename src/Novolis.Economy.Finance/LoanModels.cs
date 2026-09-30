using Novolis.Economy;
using Novolis.Economy.Abstractions;

namespace Novolis.Economy.Finance;

/// <summary>Lifecycle of an inter-firm term loan.</summary>
public enum LoanStatus
{
  /// <summary>Accruing; not yet fully repaid.</summary>
  Active = 0,
  /// <summary>Missed required repayment at term.</summary>
  Defaulted = 1,
  /// <summary>Principal and accrued interest cleared.</summary>
  Closed = 2,
}
