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

/// <summary>1. Apply policies and opening conditions.</summary>
public sealed class ApplyPolicyStep : IBoundedPeriodStep
{
    public string Name => "01_ApplyPolicy";

    public BoundedPeriodState Execute(BoundedPeriodState current)
    {
        // Reset period scratch + flow ledger; advance period counter at start.
        var state = current with
        {
            Period = checked(current.Period + 1),
            Flows = PeriodFlowLedger.Empty,
            Scratch = BoundedPeriodScratch.Empty
        };

        var policy = state.Policy;
        var fiscal = state.Specification.Fiscal;
        var householdTaxRate = fiscal.HouseholdTaxRate > 0m
            ? fiscal.HouseholdTaxRate
            : policy.HouseholdTaxRate;
        var firmTaxRate = fiscal.FirmTaxRate > 0m
            ? fiscal.FirmTaxRate
            : policy.FirmTaxRate;
        if (policy.TransferPerHousehold.Amount <= 0m &&
            householdTaxRate <= 0m &&
            firmTaxRate <= 0m)
            return state;

        // Find a State entity to act as fiscal counterparty
        var stateEntity = state.Entities.Values.FirstOrDefault(e => e.Kind == CoreLegalEntityKind.State);
        if (stateEntity is null)
            return state;

        // Household transfers (money-conserving: State → household entity or cohort cash)
        if (policy.TransferPerHousehold.Amount > 0m)
        {
            foreach (var cohort in state.Cohorts.Values)
            {
                var total = Money.From(policy.TransferPerHousehold.Amount * cohort.HouseholdCount);
                if (total.Amount <= 0m)
                    continue;
                if (cohort.HouseholdEntityId is { } hid && state.Entities.ContainsKey(hid))
                {
                    if (CashLedger.Balance(state, stateEntity.Id).Amount + 1e-12m < total.Amount)
                        break;
                    state = state.WithEconomy(
                        CashLedger.Transfer(state, stateEntity.Id, hid, total));
                    state = state.WithFlows(state.Flows.RecordTransfer(total));
                }
                else
                {
                    // Credit cash-per-household when no entity link (still debit State)
                    if (CashLedger.Balance(state, stateEntity.Id).Amount + 1e-12m < total.Amount)
                        break;
                    state = state.WithEconomy(
                        CashLedger.Debit(state, stateEntity.Id, total));
                    var cohorts = new Dictionary<CohortId, HouseholdCohort>(state.Cohorts)
                    {
                        [cohort.Id] = cohort with
                        {
                            CashPerHousehold = cohort.CashPerHousehold + policy.TransferPerHousehold
                        }
                    };
                    state = state with { Cohorts = cohorts };
                    state = state.WithFlows(state.Flows.RecordTransfer(total));
                }
            }
        }

        return state;
    }
}
