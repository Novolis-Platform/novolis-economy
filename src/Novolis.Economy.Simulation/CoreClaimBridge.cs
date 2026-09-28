using Novolis.Economy.Core;
using Novolis.Economy.Core.Finance;
using Novolis.Economy.Finance;

namespace Novolis.Economy.Simulation;

/// <summary>
/// Projects operational loan lifecycle changes into Core's authoritative claim
/// book. The Finance loan remains process state while it is being migrated.
/// </summary>
public static class CoreClaimBridge
{
    /// <summary>Write the current outstanding operational loan claim to Core.</summary>
    public static void SyncLoan(EconomyWorld world, Finance.Loan loan)
    {
        var entities = new Dictionary<LegalEntityId, Core.LegalEntity>(world.CoreState.Entities);
        EnsureEntity(entities, loan.LenderFirmId);
        EnsureEntity(entities, loan.BorrowerFirmId);
        world.CoreState = world.CoreState with { Entities = entities };

        var daysRemaining = Math.Max(
            0,
            (int)Math.Ceiling((loan.DueAt.HourIndex - loan.OriginatedAt.HourIndex) / 24d));
        var status = loan.Status switch
        {
            Finance.LoanStatus.Active => Core.LoanStatus.Performing,
            Finance.LoanStatus.Defaulted => Core.LoanStatus.Defaulted,
            Finance.LoanStatus.Closed => Core.LoanStatus.Repaid,
            _ => Core.LoanStatus.Performing
        };

        var claim = new FinancialClaim(
            ClaimId.From(loan.Id.Value),
            EconomicEntityId.From(loan.LenderFirmId.Value),
            EconomicEntityId.From(loan.BorrowerFirmId.Value),
            new AssetAmount(world.CoreState.MonetaryAssetId, loan.PrincipalRemaining.Amount),
            loan.AnnualInterestRate / 365m,
            daysRemaining,
            status);

        world.CoreState = ClaimLedger.Upsert(world.CoreState, claim);
    }

    private static void EnsureEntity(
        IDictionary<LegalEntityId, Core.LegalEntity> entities,
        FirmId firmId)
    {
        var id = firmId.AsCore();
        entities.TryAdd(id, new Core.LegalEntity(id, Core.LegalEntityKind.Firm, Money.Zero));
    }
}
