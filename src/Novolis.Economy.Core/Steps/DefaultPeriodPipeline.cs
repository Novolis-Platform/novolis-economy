namespace Novolis.Economy.Core.Steps;

/// <summary>Default 16-step period pipeline in SPEC §20 order.</summary>
public static class DefaultPeriodPipeline
{
    /// <summary>Ordered steps for <see cref="EconomyEngine"/>.</summary>
    public static IReadOnlyList<IEconomyStep> Create() =>
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

    /// <summary>Engine wired to the default pipeline.</summary>
    public static EconomyEngine CreateEngine() => new(Create());
}
