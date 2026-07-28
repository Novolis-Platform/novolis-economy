using Novolis.Economy.Core.Finance;

namespace Novolis.Economy.Core.Extensions;

/// <summary>
/// Project accounting-shaped reports from Core stocks and flows.
/// Pure reads — does not mutate state or post to ops FirmLedgers.
/// </summary>
public static class ProjectedBooksExtensions
{
    /// <summary>Value holdings at posted prices; unpriced quantity reported separately.</summary>
    public static (Money Valued, decimal UnpricedQuantity) HoldingsValuation(
        this EconomyState state,
        LegalEntityId owner)
    {
        decimal valued = 0m;
        decimal unpriced = 0m;
        foreach (var h in state.Holdings.Values.Where(x => x.Owner.Equals(owner)))
        {
            var key = EconomyState.PriceKey(h.RegionId, h.ResourceId);
            if (state.PostedPrices.TryGetValue(key, out var price) && price.UnitPrice.Amount > 0m)
                valued += h.Quantity * price.UnitPrice.Amount;
            else
                unpriced += h.Quantity;
        }

        return (Money.From(valued), unpriced);
    }

    /// <summary>Projected BS for one entity.</summary>
    public static ProjectedBalanceSheet ProjectedBooks(this EconomyState state, LegalEntityId id)
    {
        if (!state.Entities.TryGetValue(id, out var entity))
            throw new InvalidOperationException($"Unknown entity {id}.");

        var cash = entity.Cash;
        var depositsHeld = DepositLedger.TotalFor(state, id);
        var loansRecv = Money.From(
            state.Loans.Values
                .Where(l => l.Lender.Equals(id) && l.Status is LoanStatus.Performing or LoanStatus.Delinquent)
                .Sum(l => l.PrincipalOutstanding.Amount));
        var loansPay = Money.From(
            state.Loans.Values
                .Where(l => l.Borrower.Equals(id) && l.Status is LoanStatus.Performing or LoanStatus.Delinquent)
                .Sum(l => l.PrincipalOutstanding.Amount));
        var obRecv = Money.From(
            state.Obligations
                .Where(o => o.Creditor.Equals(id) && o.Status is ObligationStatus.Pending or ObligationStatus.Delinquent)
                .Sum(o => o.Amount.Amount));
        var obPay = Money.From(
            state.Obligations
                .Where(o => o.Debtor.Equals(id) && o.Status is ObligationStatus.Pending or ObligationStatus.Delinquent)
                .Sum(o => o.Amount.Amount));

        var (holdingsValued, unpriced) = state.HoldingsValuation(id);

        var depositLiab = entity.Kind == LegalEntityKind.Bank
            ? Money.From(state.Deposits.Where(d => d.Bank.Equals(id)).Sum(d => d.Balance.Amount))
            : Money.Zero;

        var undrawn = Money.From(
            state.CreditFacilities.Values
                .Where(f => f.Borrower.Equals(id) && f.IsCommitted)
                .Sum(f => f.Available.Amount));

        var assets = Money.From(
            cash.Amount + depositsHeld.Amount + loansRecv.Amount + obRecv.Amount + holdingsValued.Amount);
        var liabilities = Money.From(depositLiab.Amount + loansPay.Amount + obPay.Amount);
        var net = Money.From(assets.Amount - liabilities.Amount);

        return new ProjectedBalanceSheet(
            Id: id,
            Kind: entity.Kind,
            Cash: cash,
            DepositsHeld: depositsHeld,
            LoansReceivable: loansRecv,
            ObligationsReceivable: obRecv,
            HoldingsValued: holdingsValued,
            HoldingsUnpricedQuantity: unpriced,
            DepositLiabilities: depositLiab,
            LoansPayable: loansPay,
            ObligationsPayable: obPay,
            UndrawnCommittedCredit: undrawn,
            TotalAssets: assets,
            TotalLiabilities: liabilities,
            NetWorth: net);
    }

    /// <summary>Projected BS for every entity.</summary>
    public static IReadOnlyList<ProjectedBalanceSheet> ProjectedEntityBooks(this EconomyState state) =>
        state.Entities.Keys
            .OrderBy(id => state.Entities[id].Kind.ToString())
            .ThenBy(id => id.Value)
            .Select(state.ProjectedBooks)
            .ToList();

    /// <summary>Last-period flow appropriation (not firm GAAP revenue).</summary>
    public static ProjectedPeriodIncome ProjectedPeriodIncome(this EconomyState state)
    {
        var f = state.Flows;
        return new ProjectedPeriodIncome(
            f.MoneyCreated,
            f.MoneyDestroyed,
            f.NetMoneyCreated,
            f.WagesAccrued,
            f.TaxCollected,
            f.TransfersPaid,
            f.ObligationsPaid,
            f.ProductionOutputValue);
    }

    /// <summary>Sectoral matrix by <see cref="LegalEntityKind"/>.</summary>
    public static IReadOnlyList<SectoralBooksRow> SectoralBooks(this EconomyState state)
    {
        var books = state.ProjectedEntityBooks();
        return books
            .GroupBy(b => b.Kind)
            .OrderBy(g => g.Key.ToString())
            .Select(g => new SectoralBooksRow(
                Kind: g.Key,
                EntityCount: g.Count(),
                Cash: Money.From(g.Sum(x => x.Cash.Amount)),
                DepositsHeld: Money.From(g.Sum(x => x.DepositsHeld.Amount)),
                DepositLiabilities: Money.From(g.Sum(x => x.DepositLiabilities.Amount)),
                LoansReceivable: Money.From(g.Sum(x => x.LoansReceivable.Amount)),
                LoansPayable: Money.From(g.Sum(x => x.LoansPayable.Amount)),
                ObligationsReceivable: Money.From(g.Sum(x => x.ObligationsReceivable.Amount)),
                ObligationsPayable: Money.From(g.Sum(x => x.ObligationsPayable.Amount)),
                HoldingsValued: Money.From(g.Sum(x => x.HoldingsValued.Amount)),
                HoldingsUnpricedQuantity: g.Sum(x => x.HoldingsUnpricedQuantity),
                NetWorth: Money.From(g.Sum(x => x.NetWorth.Amount))))
            .ToList();
    }

    /// <summary>Full projected accounts for dashboards / Sins.</summary>
    public static ProjectedAccountsSnapshot ProjectedAccounts(this EconomyState state)
    {
        var entities = state.ProjectedEntityBooks();
        return new ProjectedAccountsSnapshot(
            Sectors: state.SectoralBooks(),
            Entities: entities,
            LastPeriod: state.ProjectedPeriodIncome(),
            AggregateNetWorth: Money.From(entities.Sum(e => e.NetWorth.Amount)),
            AggregateHoldingsUnpricedQuantity: entities.Sum(e => e.HoldingsUnpricedQuantity));
    }
}
