using Novolis.Economy;

namespace Novolis.Economy.Agents;

/// <summary>Working-capital lending thresholds for a treasury firm.</summary>
public sealed record TreasuryFirmAgentPolicy(
  IReadOnlyList<FirmId> EligibleBorrowers,
  decimal CashFloorToLend,
  decimal BorrowerCashFloor,
  Money LoanPrincipal,
  decimal AnnualInterestRate,
  long TermHours,
  int MaxActiveLoansToBorrower = 1);

/// <summary>
/// Holds a cash floor and originates small term loans to firms below a cash floor.
/// Uses Finance <c>OriginateLoan</c> — heuristic only.
/// </summary>
public sealed class TreasuryFirmAgent : IEconomicAgent
{
  private readonly TreasuryFirmAgentPolicy _policy;

  /// <summary>Creates the agent.</summary>
  public TreasuryFirmAgent(FirmId firmId, TreasuryFirmAgentPolicy policy)
  {
    FirmId = firmId;
    _policy = policy;
  }

  /// <inheritdoc />
  public FirmId FirmId { get; }

  /// <inheritdoc />
  public string LastDecision { get; private set; } = "treasury idle";

  /// <inheritdoc />
  public void Tick(AgentContext context)
  {
    var world = context.World;
    if (!world.Ledgers.TryGetValue(FirmId, out var treasury)
        || treasury.Cash.Amount < _policy.CashFloorToLend + _policy.LoanPrincipal.Amount)
    {
      LastDecision = "treasury thin";
      return;
    }

    foreach (var borrower in _policy.EligibleBorrowers.OrderBy(f => f.Value))
    {
      if (!world.Ledgers.TryGetValue(borrower, out var ledger)
          || ledger.Cash.Amount >= _policy.BorrowerCashFloor)
      {
        continue;
      }

      var active = world.Loans.Count(l =>
        l.LenderFirmId.Equals(FirmId)
        && l.BorrowerFirmId.Equals(borrower)
        && l.Status == Novolis.Economy.Finance.LoanStatus.Active);
      if (active >= _policy.MaxActiveLoansToBorrower)
      {
        continue;
      }

      // Skip borrowers already in default with this lender.
      if (world.Loans.Any(l =>
            l.LenderFirmId.Equals(FirmId)
            && l.BorrowerFirmId.Equals(borrower)
            && l.Status == Novolis.Economy.Finance.LoanStatus.Defaulted))
      {
        continue;
      }

      context.Enqueue(new OriginateLoan(
        FirmId, borrower, _policy.LoanPrincipal, _policy.AnnualInterestRate, _policy.TermHours));
      LastDecision = $"lend {_policy.LoanPrincipal.Amount:0} → {borrower.Value.ToString("N")[..8]}";
      return;
    }

    LastDecision = "treasury idle";
  }
}
