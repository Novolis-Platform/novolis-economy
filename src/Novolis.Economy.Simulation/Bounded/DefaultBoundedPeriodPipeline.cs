using Novolis.Economy.Simulation.Bounded;

namespace Novolis.Economy.Simulation.Bounded;

/// <summary>Default 16-step period pipeline in SPEC §20 order.</summary>
public static class DefaultBoundedPeriodPipeline
{
    /// <summary>Ordered steps for <see cref="BoundedPeriodEngine"/>.</summary>
    public static IReadOnlyList<IBoundedPeriodStep> Create() =>
    [
        new ApplyPolicyStep(),
        new CalculateLaborSupplyStep(),
        new AllocateLaborStep(),
        new DetermineProductionStep(),
        new ApplyProductionStep(),
        new ResolveDemandStep(),
        new MatchBuyersSellersStep(),
        new TransferOwnershipPaymentsStep(),
        new ProcessTransfersStep(),
        new CreateObligationsStep(),
        new SettleObligationsStep(),
        new DrawCreditStep(),
        new MarkDelinquencyStep(),
        new DistributeDividendsStep(),
        new HouseholdConsumeMigrateStep(),
        new ReconcileStep()
    ];

    /// <summary>Engine wired to the default bounded pipeline.</summary>
    public static BoundedPeriodEngine CreateEngine(
      EconomyModelSpecification? specification = null) =>
      new(Create(), specification);
}
