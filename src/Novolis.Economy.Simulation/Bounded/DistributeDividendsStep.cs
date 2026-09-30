using Novolis.Economy.Core;
using Novolis.Economy.Core.Finance;
using Novolis.Economy.Core.Holdings;
using Novolis.Economy.Core.Invariants;
using Novolis.Economy.Core.Labor;
using Novolis.Economy.Core.Production;
using Novolis.Economy.Core.Transport;
using Novolis.Economy.Core.Transactions;
using CoreLegalEntityKind = Novolis.Economy.Core.LegalEntityKind;

using Novolis.Economy.Simulation.Bounded;

namespace Novolis.Economy.Simulation.Bounded;

/// <summary>14. Distribute dividends from firm cash above a retention floor.</summary>
public sealed class DistributeDividendsStep : IBoundedPeriodStep
{
    public string Name => "14_DistributeDividends";

    public BoundedPeriodState Execute(BoundedPeriodState current)
    {
        var state = current;
        var retention = state.Specification.Dividends.RetainedCashFloor;

        foreach (var firm in state.Entities.Values.Where(e => e.Kind == CoreLegalEntityKind.Firm))
        {
            var distributable = CashLedger.Balance(state, firm.Id).Amount - retention;
            if (distributable <= 0m)
                continue;

            var holdings = state.ShareHoldings.Where(h => h.Issuer.Equals(firm.Id) && h.Units > 0m).ToList();
            var totalUnits = holdings.Sum(h => h.Units);
            if (totalUnits <= 0m)
                continue;

            foreach (var h in holdings)
            {
                var share = Money.From(distributable * (h.Units / totalUnits));
                if (share.Amount <= 0m)
                    continue;
                state = state.WithEconomy(ObligationEngine.Create(
                    state, firm.Id, h.Owner, share, state.Period, ObligationKind.Dividend));
            }
        }

        // Immediately settle dividends created this period
        return state.WithEconomy(ObligationEngine.SettleDue(state));
    }
}
