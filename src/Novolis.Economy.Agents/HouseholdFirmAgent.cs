using Novolis.Economy;

namespace Novolis.Economy.Agents;

/// <summary>Comfort invest / lend policy for a household cohort firm.</summary>
public sealed record HouseholdFirmAgentPolicy(
  FirmId? PreferredBorrower = null,
  FirmId? PreferredIssuer = null,
  Money? LoanPrincipal = null,
  decimal AnnualInterestRate = 0.08m,
  long TermHours = 72,
  decimal PurchaseFraction = 0.01m,
  Money? PurchasePrice = null,
  int MaxActiveLoans = 1);

/// <summary>
/// Invests / lends only when cohort <c>BudgetRemaining</c> is strictly above comfort.
/// Spendable liquid is budget only — household ledger cash is ignored.
/// </summary>
public sealed class HouseholdFirmAgent : IEconomicAgent
{
  private readonly HouseholdFirmAgentPolicy _policy;

  /// <summary>Creates the agent.</summary>
  public HouseholdFirmAgent(FirmId firmId, HouseholdFirmAgentPolicy? policy = null)
  {
    FirmId = firmId;
    _policy = policy ?? new HouseholdFirmAgentPolicy();
  }

  /// <inheritdoc />
  public FirmId FirmId { get; }

  /// <inheritdoc />
  public string LastDecision { get; private set; } = "household idle";

  /// <inheritdoc />
  public void Tick(AgentContext context)
  {
    var world = context.World;
    var cohort = world.FindCohortByHousehold(FirmId);
    if (cohort is null)
    {
      LastDecision = "no cohort";
      return;
    }

    if (!world.IsAboveComfort(cohort))
    {
      LastDecision = "comfort hold";
      return;
    }

    var floor = world.ComfortFloor(cohort).Amount;
    var surplus = cohort.BudgetRemaining.Amount - floor;
    if (surplus <= 0m)
    {
      LastDecision = "comfort hold";
      return;
    }

    // Prefer a small loan when a borrower is configured and surplus covers it.
    var loanPrincipal = _policy.LoanPrincipal ?? Money.From(Math.Min(25m, Math.Floor(surplus * 0.25m)));
    if (_policy.PreferredBorrower is { } borrower
        && loanPrincipal.Amount > 0m
        && cohort.BudgetRemaining.Amount - loanPrincipal.Amount > floor
        && !world.IsCreditFrozen(borrower))
    {
      var active = world.Loans.Count(l =>
        l.LenderFirmId.Equals(FirmId)
        && l.BorrowerFirmId.Equals(borrower)
        && l.Status == Novolis.Economy.Finance.LoanStatus.Active);
      if (active < _policy.MaxActiveLoans)
      {
        context.Enqueue(new OriginateLoan(
          FirmId,
          borrower,
          loanPrincipal,
          _policy.AnnualInterestRate,
          _policy.TermHours));
        LastDecision = $"lend {loanPrincipal.Amount:0.##}";
        return;
      }
    }

    var price = _policy.PurchasePrice ?? Money.From(Math.Min(20m, Math.Floor(surplus * 0.2m)));
    if (_policy.PreferredIssuer is { } issuer
        && price.Amount > 0m
        && _policy.PurchaseFraction > 0m
        && cohort.BudgetRemaining.Amount - price.Amount > floor
        && world.CanIssueShares(issuer))
    {
      context.Enqueue(new PurchaseOwnership(
        issuer, FirmId, _policy.PurchaseFraction, price));
      LastDecision = $"invest {price.Amount:0.##}";
      return;
    }

    LastDecision = "above comfort idle";
  }
}
