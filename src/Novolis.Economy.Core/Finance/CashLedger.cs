using Novolis.Economy.Core.Holdings;
using Novolis.Economy.Core.Transactions;

namespace Novolis.Economy.Core.Finance;

/// <summary>Cash mutations on legal entities.</summary>
public static class CashLedger
{
    /// <summary>Read the authoritative monetary position, with legacy cash fallback.</summary>
    public static Money Balance(EconomyState state, LegalEntityId id)
    {
        if (!state.Entities.TryGetValue(id, out var entity))
            throw new InvalidOperationException($"Unknown entity {id}.");

        var quantity = PositionLedger.GetQuantity(
            state,
            EconomicIdentity.For(id),
            region: null,
            state.MonetaryAssetId);
        return state.PositionState.ContainsKey(
            PositionLedger.Key(EconomicIdentity.For(id), null, state.MonetaryAssetId))
            ? Money.From(quantity)
            : entity.Cash;
    }

    /// <summary>Set entity cash.</summary>
    public static EconomyState SetCash(EconomyState state, LegalEntityId id, Money cash)
    {
        if (!state.Entities.TryGetValue(id, out var entity))
            throw new InvalidOperationException($"Unknown entity {id}.");

        var current = Balance(state, id);
        state = EnsurePosition(state, id);
        var owner = EconomicIdentity.For(id);

        state = EconomicTransactionEngine.Apply(
            state,
            EconomicTransaction.Create(
                state,
                [
                    new PositionChange(
                        owner,
                        state.MonetaryAssetId,
                        cash.Amount - current.Amount,
                        Region: null)
                ],
                "monetary-position-change"));

        var entities = new Dictionary<LegalEntityId, LegalEntity>(state.Entities)
        {
            [id] = entity with { Cash = cash }
        };
        return state with { Entities = entities };
    }

    /// <summary>Materialize a legacy cash projection as the monetary position.</summary>
    public static EconomyState EnsurePosition(EconomyState state, LegalEntityId id)
    {
        if (!state.Entities.TryGetValue(id, out var entity))
            throw new InvalidOperationException($"Unknown entity {id}.");

        var owner = EconomicIdentity.For(id);
        var key = PositionLedger.Key(owner, region: null, state.MonetaryAssetId);
        if (state.PositionState.ContainsKey(key) || entity.Cash.Amount <= 0m)
            return state;

        return PositionLedger.Apply(
            state,
            owner,
            region: null,
            state.MonetaryAssetId,
            entity.Cash.Amount);
    }

    /// <summary>Refreshes the legacy entity-cash projection from monetary positions.</summary>
    public static EconomyState ProjectEntityCash(
        EconomyState state,
        IReadOnlySet<LegalEntityId>? ownersToProject = null)
    {
        var entities = new Dictionary<LegalEntityId, LegalEntity>(state.Entities);
        var changed = false;
        foreach (var entity in state.Entities.Values)
        {
            var key = PositionLedger.Key(
                EconomicIdentity.For(entity.Id),
                region: null,
                state.MonetaryAssetId);
            var hasPosition = state.PositionState.TryGetValue(key, out var position);
            if (!hasPosition &&
                (ownersToProject is null || !ownersToProject.Contains(entity.Id)))
                continue;

            var cash = hasPosition ? Money.From(position.Quantity) : Money.Zero;
            if (entity.Cash != cash)
            {
                entities[entity.Id] = entity with { Cash = cash };
                changed = true;
            }
        }

        return changed ? state with { Entities = entities } : state;
    }

    /// <summary>Add cash to entity.</summary>
    public static EconomyState Credit(EconomyState state, LegalEntityId id, Money amount)
    {
        return SetCash(state, id, Balance(state, id) + amount);
    }

    /// <summary>Remove cash; throws if insufficient.</summary>
    public static EconomyState Debit(EconomyState state, LegalEntityId id, Money amount)
    {
        var balance = Balance(state, id);
        if (balance.Amount + 1e-12m < amount.Amount)
            throw new InvalidOperationException(
                $"Insufficient cash for {id}: have {balance}, need {amount}.");
        return SetCash(state, id, balance - amount);
    }

    /// <summary>Transfer cash; money-conserving.</summary>
    public static EconomyState Transfer(EconomyState state, LegalEntityId from, LegalEntityId to, Money amount)
    {
        if (amount.Amount <= 0m)
            return state;
        if (from.Equals(to))
            return state;

        state = EnsurePosition(state, from);
        state = EnsurePosition(state, to);
        if (Balance(state, from).Amount + 1e-12m < amount.Amount)
            throw new InvalidOperationException(
                $"Insufficient cash for {from}: have {Balance(state, from)}, need {amount}.");

        return EconomicTransactionEngine.Apply(
            state,
            EconomicTransaction.Create(
                state,
                [
                    new PositionChange(
                        EconomicIdentity.For(from),
                        state.MonetaryAssetId,
                        -amount.Amount,
                        Region: null),
                    new PositionChange(
                        EconomicIdentity.For(to),
                        state.MonetaryAssetId,
                        amount.Amount,
                        Region: null)
                ],
                "cash-transfer"));
    }

    /// <summary>Try debit; returns false without mutation when insufficient.</summary>
    public static bool TryDebit(ref EconomyState state, LegalEntityId id, Money amount)
    {
        if (!state.Entities.ContainsKey(id) || Balance(state, id).Amount + 1e-12m < amount.Amount)
            return false;
        state = Debit(state, id, amount);
        return true;
    }
}
