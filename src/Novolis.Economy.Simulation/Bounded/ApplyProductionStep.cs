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

/// <summary>5. Add produced resources to owner holdings.</summary>
public sealed class ApplyProductionStep : IBoundedPeriodStep
{
    public string Name => "05_ApplyProduction";

    public BoundedPeriodState Execute(BoundedPeriodState current)
    {
        var state = current;
        foreach (var (activityId, runCount) in current.Scratch.ActualRuns)
        {
            if (runCount <= 0m || !current.Activities.TryGetValue(activityId, out var activity))
                continue;
            state = state.WithEconomy(
                ProductionCalculator.ApplyRuns(state, activity, runCount));
            foreach (var input in activity.Recipe.Inputs)
            {
                if (input.Quantity <= 0m)
                    continue;
                state = state.WithFlows(
                    state.Flows.RecordConsumedQuantity(
                        state.AssetFor(input.ResourceId),
                        input.Quantity * runCount));
            }

            foreach (var output in activity.Recipe.Outputs)
            {
                if (output.Quantity <= 0m)
                    continue;
                state = state.WithFlows(
                    state.Flows.RecordProductionQuantity(
                        state.AssetFor(output.ResourceId),
                        output.Quantity * runCount));
            }
        }

        return state;
    }
}
