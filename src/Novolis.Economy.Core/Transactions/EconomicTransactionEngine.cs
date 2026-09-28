using Novolis.Economy.Core.Holdings;
using Novolis.Economy.Core.Finance;
using Novolis.Economy.Primitives;

namespace Novolis.Economy.Core.Transactions;

/// <summary>Applies Core economic transactions atomically.</summary>
public static class EconomicTransactionEngine
{
    /// <summary>
    /// Applies all position changes in order. Because the resulting state is
    /// returned only after every effect succeeds, a failed transaction does
    /// not escape a partial state.
    /// </summary>
    public static EconomyState Apply(
        EconomyState state,
        EconomicTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        var next = state;
        foreach (var effect in transaction.Effects)
        {
            next = effect switch
            {
                PositionChange change => PositionLedger.Apply(
                    next,
                    change.Owner,
                    change.Region,
                    change.Asset,
                    change.Delta),
                CreateClaim create => ClaimLedger.Upsert(next, create.Claim),
                SettleClaim settle => Settle(next, settle),
                _ => throw new InvalidOperationException(
                    $"Unsupported economic effect {effect.GetType().Name}.")
            };
        }

        var monetaryOwners = transaction.Effects
            .OfType<PositionChange>()
            .Where(change => change.Asset.Equals(next.MonetaryAssetId))
            .Select(change => EconomicIdentity.ToLegalEntityId(change.Owner))
            .ToHashSet();
        return CashLedger.ProjectEntityCash(next, monetaryOwners);
    }

    private static EconomyState Settle(EconomyState state, SettleClaim settle)
    {
        var claim = ClaimLedger.Snapshot(state).GetValueOrDefault(settle.ClaimId)
            ?? throw new InvalidOperationException($"Unknown claim {settle.ClaimId}.");
        if (!claim.Principal.Asset.Equals(settle.Amount.Asset))
            throw new InvalidOperationException("Claim settlement denomination does not match.");
        if (settle.Amount.Quantity > claim.Principal.Quantity + 1e-12m)
            throw new InvalidOperationException("Claim settlement exceeds outstanding principal.");

        var remaining = claim.Principal.Quantity - settle.Amount.Quantity;
        return ClaimLedger.Upsert(
            state,
            claim with
            {
                Principal = new AssetAmount(claim.Principal.Asset, Math.Max(0m, remaining)),
                Status = remaining <= 1e-12m ? LoanStatus.Repaid : claim.Status
            });
    }
}
