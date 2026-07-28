using Novolis.Economy.Core.Holdings;
using Novolis.Economy.Core.Labor;

namespace Novolis.Economy.Core.Production;

/// <summary>Min-constraint production (SPEC §6).</summary>
public static class ProductionCalculator
{
    /// <summary>
    /// Actual runs = min(installed, floor(space/recipe), floor(labor/recipe), floor(inputs)).
    /// </summary>
    public static decimal ActualRuns(
        EconomyState state,
        Activity activity,
        decimal laborAlreadyCommitted = 0m)
    {
        if (activity.InstalledCapacity <= 0m)
            return 0m;

        var recipe = activity.Recipe;
        var byInstall = activity.InstalledCapacity;

        var bySpace = recipe.ProductionSpacePerRun <= 0m
            ? byInstall
            : (state.Regions.TryGetValue(activity.RegionId, out var region)
                ? region.ProductionCapacity / recipe.ProductionSpacePerRun
                : 0m);

        var laborAvail = LaborSupply.Remaining(state, activity.RegionId, laborAlreadyCommitted);
        var byLabor = recipe.LaborHoursPerRun <= 0m
            ? byInstall
            : laborAvail / recipe.LaborHoursPerRun;

        var byInputs = byInstall;
        foreach (var input in recipe.Inputs)
        {
            if (input.Quantity <= 0m)
                continue;
            var have = HoldingLedger.GetQuantity(state, activity.Operator, activity.RegionId, input.ResourceId);
            byInputs = Math.Min(byInputs, have / input.Quantity);
        }

        return Math.Max(0m, Math.Floor(Math.Min(Math.Min(byInstall, bySpace), Math.Min(byLabor, byInputs))));
    }

    /// <summary>Consume inputs and produce outputs for <paramref name="runs"/> activity runs.</summary>
    public static EconomyState ApplyRuns(EconomyState state, Activity activity, decimal runs)
    {
        if (runs <= 0m)
            return state;

        foreach (var input in activity.Recipe.Inputs)
        {
            if (input.Quantity <= 0m)
                continue;
            state = HoldingLedger.Debit(
                state,
                activity.Operator,
                activity.RegionId,
                input.ResourceId,
                input.Quantity * runs);
        }

        foreach (var output in activity.Recipe.Outputs)
        {
            if (output.Quantity <= 0m)
                continue;
            state = HoldingLedger.Credit(
                state,
                activity.Operator,
                activity.RegionId,
                output.ResourceId,
                output.Quantity * runs);
        }

        return state;
    }
}
