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

/// <summary>15. Household consumption of holdings + simple migration toward remaining living capacity.</summary>
public sealed class HouseholdConsumeMigrateStep : IBoundedPeriodStep
{
    public string Name => "15_HouseholdConsumeMigrate";

    public BoundedPeriodState Execute(BoundedPeriodState current)
    {
        var state = current;

        foreach (var cohort in state.Cohorts.Values)
        {
            if (cohort.HouseholdEntityId is not { } hid)
                continue;
            var consumeRate = Math.Clamp(cohort.Profile.ConsumptionWeight, 0m, 1m);
            var holdings = PositionLedger.ResourceView(state)
                .Where(h => h.Owner.Equals(hid) && h.RegionId.Equals(cohort.RegionId))
                .ToList();
            foreach (var h in holdings)
            {
                if (!state.Resources.TryGetValue(h.ResourceId, out var res) ||
                    res.Kind != ResourceKind.ConsumerGood)
                    continue;
                var eat = h.Quantity * consumeRate;
                if (eat <= 0m)
                    continue;
                state = state.WithEconomy(HoldingLedger.Debit(
                    state, hid, h.RegionId, h.ResourceId, eat));
            }
        }

        // Migration: living overflow OR tax-sensitive mobility when MigrationPreference is high.
        var cohorts = new Dictionary<CohortId, HouseholdCohort>(state.Cohorts);
        var migrated = 0;
        var taxRate = state.Specification.Fiscal.HouseholdTaxRate > 0m
            ? state.Specification.Fiscal.HouseholdTaxRate
            : state.Policy.HouseholdTaxRate;
        var taxPush = taxRate >= state.Specification.Migration.TaxPushThreshold;

        foreach (var cohort in state.Cohorts.Values.ToList())
        {
            if (cohort.Profile.MigrationPreference < 0.5m || cohort.HouseholdCount <= 0)
                continue;
            if (!state.Regions.TryGetValue(cohort.RegionId, out var home))
                continue;
            if (!cohorts.TryGetValue(cohort.Id, out var live) || live.HouseholdCount <= 0)
                continue;

            // Recompute living using current cohort map
            var tempState = state with { Cohorts = cohorts };
            var overflow = RegionCapacity.RemainingLiving(tempState, home) < 0;
            var taxMotivated = taxPush &&
                               cohort.Profile.MigrationPreference >=
                               state.Specification.Migration.MinimumMigrationPreference;
            if (!overflow && !taxMotivated)
                continue;

            var candidates = state.Regions.Values
                .Where(r => !r.Id.Equals(live.RegionId))
                .Select(r => (Region: r, Slack: RegionCapacity.RemainingLiving(tempState, r)))
                .Where(x => x.Slack > 0)
                .OrderByDescending(x => x.Slack)
                .ToList();
            if (candidates.Count == 0)
                continue;

            var target = candidates[0].Region;
            var slack = (int)candidates[0].Slack;
            int move;
            if (overflow)
                move = Math.Min(live.HouseholdCount, slack);
            else
                move = Math.Min(Math.Max(1, live.HouseholdCount / 2), slack);

            if (move <= 0)
                continue;

            if (move >= live.HouseholdCount)
            {
                cohorts[live.Id] = live with { RegionId = target.Id };
                migrated += live.HouseholdCount;
            }
            else
            {
                cohorts[live.Id] = live with { HouseholdCount = live.HouseholdCount - move };
                var splitId = DeterministicIds.CohortSplitIdFor(
                    state,
                    live.Id,
                    target.Id,
                    move);
                cohorts[splitId] = live with { Id = splitId, RegionId = target.Id, HouseholdCount = move };
                migrated += move;
            }
        }

        return state with
        {
            Cohorts = cohorts,
            Scratch = state.Scratch with { HouseholdsMigrated = migrated },
        };
    }
}
