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

/// <summary>10. Create wage, tax, interest, and insurance obligations.</summary>
public sealed class CreateObligationsStep : IBoundedPeriodStep
{
    public string Name => "10_CreateObligations";

    public BoundedPeriodState Execute(BoundedPeriodState current)
    {
        var state = current;
        var due = state.Period;

        // Wages: firm → linked household for labor used
        foreach (var (activityId, runs) in state.Scratch.ActualRuns)
        {
            if (runs <= 0m || !state.Activities.TryGetValue(activityId, out var activity))
                continue;
            var hours = runs * activity.Recipe.LaborHoursPerRun;
            if (hours <= 0m)
                continue;
            var wage = Money.From(hours * state.Policy.WagePerLaborHour.Amount);
            var creditor = FindHouseholdCreditor(state, activity.RegionId);
            if (creditor is null)
                continue;
            state = state.WithEconomy(ObligationEngine.Create(
                state, activity.Operator, creditor.Value, wage, due, ObligationKind.Wage));
            state = state.WithFlows(state.Flows.RecordWages(wage));
        }

        // Interest on performing loans
        var loans = ClaimLedger.LoanView(state)
            .ToDictionary(loan => loan.Id);
        foreach (var loan in loans.Values.Where(l => l.Status == LoanStatus.Performing))
        {
            var interest = Money.From(loan.PrincipalOutstanding.Amount * loan.InterestRatePerPeriod);
            if (interest.Amount <= 0m)
                continue;
            state = state.WithEconomy(ObligationEngine.Create(
                state, loan.Borrower, loan.Lender, interest, due, ObligationKind.Interest));
            // Age remaining periods
            var rem = loan.RemainingPeriods - 1;
            loans[loan.Id] = rem <= 0
                ? loan with { RemainingPeriods = 0 }
                : loan with { RemainingPeriods = rem };
        }

        state = state with { Loans = loans };
        state = state.WithEconomy(ClaimLedger.SyncFromLegacyLoans(state));

        // Principal due when term expired
        foreach (var loan in loans.Values.Where(l =>
                     l.Status == LoanStatus.Performing && l.RemainingPeriods <= 0))
        {
            if (loan.PrincipalOutstanding.Amount <= 0m)
                continue;
            state = state.WithEconomy(ObligationEngine.Create(
                state,
                loan.Borrower,
                loan.Lender,
                loan.PrincipalOutstanding,
                due,
                ObligationKind.Principal));
        }

        // Insurance premiums
        foreach (var cover in state.Insurance)
        {
            if (cover.PremiumPerPeriod.Amount <= 0m)
                continue;
            state = state.WithEconomy(ObligationEngine.Create(
                state,
                cover.Insured,
                cover.Insurer,
                cover.PremiumPerPeriod,
                due,
                ObligationKind.InsurancePremium));
        }

        // Taxes: household and firm rates on cash (simple fiscal). A positive
        // model-specification rate overrides the legacy StatePolicy input.
        var treasury = state.Entities.Values.FirstOrDefault(e => e.Kind == CoreLegalEntityKind.State);
        if (treasury is not null)
        {
            var fiscal = state.Specification.Fiscal;
            var householdTaxRate = fiscal.HouseholdTaxRate > 0m
                ? fiscal.HouseholdTaxRate
                : state.Policy.HouseholdTaxRate;
            var firmTaxRate = fiscal.FirmTaxRate > 0m
                ? fiscal.FirmTaxRate
                : state.Policy.FirmTaxRate;
            foreach (var entity in state.Entities.Values)
            {
                decimal rate = entity.Kind switch
                {
                    CoreLegalEntityKind.Household => householdTaxRate,
                    CoreLegalEntityKind.Firm => firmTaxRate,
                    _ => 0m
                };
                var entityCash = CashLedger.Balance(state, entity.Id);
                if (rate <= 0m || entityCash.Amount <= 0m)
                    continue;
                var tax = Money.From(entityCash.Amount * rate);
                state = state.WithEconomy(ObligationEngine.Create(
                    state, entity.Id, treasury.Id, tax, due, ObligationKind.Tax));
                state = state.WithFlows(state.Flows.RecordTax(tax));
            }
        }

        // Insurance claims from pending losses
        foreach (var loss in state.PendingLosses)
        {
            var covers = state.Insurance
                .Where(c => c.Insured.Equals(loss.Insured) && c.Risk == loss.Risk)
                .ToList();
            foreach (var cover in covers)
            {
                var covered = Money.From(
                    Math.Max(0m, (loss.GrossLoss.Amount - cover.Deductible.Amount) * cover.CoveredFraction));
                if (covered.Amount <= 0m)
                    continue;
                state = state.WithEconomy(ObligationEngine.Create(
                    state, cover.Insurer, cover.Insured, covered, due, ObligationKind.InsuranceClaim));
            }
        }

        return state with
        {
            PendingLosses = Array.Empty<LossEvent>()
        };
    }

    private static LegalEntityId? FindHouseholdCreditor(EconomyState state, RegionId regionId)
    {
        var cohort = state.Cohorts.Values.FirstOrDefault(c =>
            c.RegionId.Equals(regionId) && c.HouseholdEntityId is not null);
        return cohort?.HouseholdEntityId;
    }
}
