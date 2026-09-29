using Novolis.Economy;
using Novolis.Economy.Agents;
using Novolis.Economy.Accounting;
using Novolis.Economy.Finance;
using Novolis.Economy.Logistics;
using Novolis.Economy.Markets;
using Novolis.Economy.Population;
using Novolis.Economy.Production;

namespace Novolis.Economy.Simulation.Phases;

public sealed class SettleInvoicesAndWagesPhase : ISimulationPhase
{
  /// <inheritdoc />
  public SimulationPhaseOrder Order => SimulationPhaseOrder.SettleInvoicesAndWages;

  /// <inheritdoc />
  public ValueTask ExecuteAsync(SimulationContext context, CancellationToken cancellationToken)
  {
    var world = context.State.World;
    var hour = context.State.Clock;

    foreach (var (firmId, accrued) in world.AccruedWages.OrderBy(kv => kv.Key.Value))
    {
      if (accrued.Amount <= 0m || !world.Ledgers.TryGetValue(firmId, out var ledger))
      {
        continue;
      }

      var pay = Money.From(Math.Min(accrued.Amount, ledger.Cash.Amount));
      if (pay.Amount <= 0m)
      {
        continue;
      }

      LedgerEngine.PayWages(ledger, pay, hour.Date);
      world.AccruedWages[firmId] = accrued - pay;
      context.State.AppendEvent(new WagesPaid(hour, firmId, pay));

      if (world.Policy.HouseholdCreditFromWages && pay.Amount > 0m)
      {
        DistributeWageCreditsForFirm(world, firmId, pay.Amount);
        context.State.AppendEvent(new HouseholdCreditsIssued(hour, firmId, pay));
      }
    }

    foreach (var invoice in world.Invoices.Where(i => !i.IsSettled).OrderBy(i => i.Id))
    {
      if (invoice.BuyerFirmId is not { } buyer || !world.Ledgers.TryGetValue(buyer, out var buyerLedger))
      {
        continue;
      }

      if (!world.Ledgers.TryGetValue(invoice.SellerFirmId, out var sellerLedger))
      {
        continue;
      }

      var pay = Money.From(Math.Min(invoice.Remaining.Amount, buyerLedger.Cash.Amount));
      if (pay.Amount <= 0m)
      {
        continue;
      }

      buyerLedger.Post(AccountRole.AccountsPayable, AccountRole.Cash, pay, hour.Date, "Invoice payment");
      sellerLedger.Post(AccountRole.Cash, AccountRole.AccountsReceivable, pay, hour.Date, "Invoice receipt");
      invoice.Remaining -= pay;
      context.State.AppendEvent(new InvoiceSettled(hour, invoice.Id, pay));
    }

    return ValueTask.CompletedTask;
  }

  /// <summary>
  /// Credits cohorts by facility area when the paying firm has area-bound facilities;
  /// otherwise population-weighted global fallback.
  /// </summary>
  public static void DistributeWageCreditsForFirm(EconomyWorld world, FirmId firmId, decimal amount)
  {
    if (amount <= 0m || world.Cohorts.Count == 0)
    {
      return;
    }

    var facilities = world.Facilities.Values.Where(f => f.FirmId.Equals(firmId)).ToList();
    var areaWeights = facilities
      .Where(f => f.Area is not null)
      .GroupBy(f => f.Area!.Value)
      .Select(g => (Area: g.Key, Weight: Math.Max(1m, g.Sum(f => f.ManufacturingCapacity.Value))))
      .ToList();

    if (areaWeights.Count == 0)
    {
      DistributeWageCreditsToCohorts(world, amount);
      return;
    }

    var totalWeight = areaWeights.Sum(a => a.Weight);
    var allocated = 0m;
    for (var i = 0; i < areaWeights.Count; i++)
    {
      var (area, weight) = areaWeights[i];
      decimal share;
      if (i == areaWeights.Count - 1)
      {
        share = amount - allocated;
      }
      else
      {
        share = Math.Round(amount * weight / totalWeight, 4, MidpointRounding.AwayFromZero);
        allocated += share;
      }

      if (share > 0m)
      {
        DistributeWageCreditsToCohortsInArea(world, area, share);
      }
    }
  }

  /// <summary>Population-weighted split within one area; falls back globally if empty.</summary>
  internal static void DistributeWageCreditsToCohortsInArea(
    EconomyWorld world,
    GeographicAreaId area,
    decimal amount)
  {
    var local = world.Cohorts.Where(c => c.Definition.Area.Equals(area)).ToList();
    if (local.Count == 0)
    {
      DistributeWageCreditsToCohorts(world, amount);
      return;
    }

    CreditCohortsPopulationWeighted(local, amount);
  }

  /// <summary>Population-weighted split; remainder to largest cohort (stable id tie-break).</summary>
  internal static void DistributeWageCreditsToCohorts(EconomyWorld world, decimal amount)
  {
    if (amount <= 0m || world.Cohorts.Count == 0)
    {
      return;
    }

    CreditCohortsPopulationWeighted(world.Cohorts, amount);
  }

  private static void CreditCohortsPopulationWeighted(IReadOnlyList<CohortState> cohorts, decimal amount)
  {
    var popTotal = cohorts.Sum(c => Math.Max(1, c.Definition.Population.Value));
    if (popTotal <= 0 || amount <= 0m)
    {
      return;
    }

    var allocated = 0m;
    var ordered = cohorts
      .OrderByDescending(c => c.Definition.Population.Value)
      .ThenBy(c => c.Definition.Id.Value)
      .ToList();

    for (var i = 0; i < ordered.Count; i++)
    {
      var c = ordered[i];
      decimal share;
      if (i == ordered.Count - 1)
      {
        share = amount - allocated;
      }
      else
      {
        share = Math.Round(
          amount * c.Definition.Population.Value / popTotal,
          4,
          MidpointRounding.AwayFromZero);
        allocated += share;
      }

      if (share > 0m)
      {
        c.BudgetRemaining = Money.From(c.BudgetRemaining.Amount + share);
      }
    }
  }
}
