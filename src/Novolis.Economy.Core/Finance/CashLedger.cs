namespace Novolis.Economy.Core.Finance;

/// <summary>Cash mutations on legal entities.</summary>
public static class CashLedger
{
    /// <summary>Set entity cash.</summary>
    public static EconomyState SetCash(EconomyState state, LegalEntityId id, Money cash)
    {
        if (!state.Entities.TryGetValue(id, out var entity))
            throw new InvalidOperationException($"Unknown entity {id}.");
        var entities = new Dictionary<LegalEntityId, LegalEntity>(state.Entities)
        {
            [id] = entity with { Cash = cash }
        };
        return state with { Entities = entities };
    }

    /// <summary>Add cash to entity.</summary>
    public static EconomyState Credit(EconomyState state, LegalEntityId id, Money amount)
    {
        var e = state.Entities[id];
        return SetCash(state, id, e.Cash + amount);
    }

    /// <summary>Remove cash; throws if insufficient.</summary>
    public static EconomyState Debit(EconomyState state, LegalEntityId id, Money amount)
    {
        var e = state.Entities[id];
        if (e.Cash.Amount + 1e-12m < amount.Amount)
            throw new InvalidOperationException(
                $"Insufficient cash for {id}: have {e.Cash}, need {amount}.");
        return SetCash(state, id, e.Cash - amount);
    }

    /// <summary>Transfer cash; money-conserving.</summary>
    public static EconomyState Transfer(EconomyState state, LegalEntityId from, LegalEntityId to, Money amount)
    {
        if (amount.Amount <= 0m)
            return state;
        state = Debit(state, from, amount);
        return Credit(state, to, amount);
    }

    /// <summary>Try debit; returns false without mutation when insufficient.</summary>
    public static bool TryDebit(ref EconomyState state, LegalEntityId id, Money amount)
    {
        if (!state.Entities.TryGetValue(id, out var e) || e.Cash.Amount + 1e-12m < amount.Amount)
            return false;
        state = Debit(state, id, amount);
        return true;
    }
}
