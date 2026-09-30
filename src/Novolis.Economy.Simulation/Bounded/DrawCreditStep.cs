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

/// <summary>12. Draw committed credit where liquidity is short.</summary>
public sealed class DrawCreditStep : IBoundedPeriodStep
{
    public string Name => "12_DrawCredit";

    public BoundedPeriodState Execute(BoundedPeriodState current)
    {
        var state = current;
        foreach (var facility in current.CreditFacilities.Values.Where(f => f.IsCommitted && f.Available.Amount > 0m))
        {
            var liq = Liquidity.Of(state, facility.Borrower);
            if (liq.Surplus.Amount >= 0m)
                continue;
            var need = Money.From(Math.Min(facility.Available.Amount, -liq.Surplus.Amount));
            if (need.Amount <= 0m)
                continue;
            try
            {
                state = state.WithEconomy(CreditEngine.DrawFacility(
                    state,
                    facility.Id,
                    need,
                    interestRatePerPeriod: state.Specification.Credit.FacilityInterestRatePerPeriod,
                    termPeriods: state.Specification.Credit.FacilityTermPeriods));
            }
            catch (InvalidOperationException)
            {
                // skip
            }
        }

        return state;
    }
}
