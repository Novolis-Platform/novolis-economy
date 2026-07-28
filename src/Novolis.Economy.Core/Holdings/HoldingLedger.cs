namespace Novolis.Economy.Core.Holdings;

/// <summary>Owner × Region × Resource holding ledger (SPEC §8).</summary>
public static class HoldingLedger
{
    /// <summary>Stable key for a holding slot.</summary>
    public static string Key(LegalEntityId owner, RegionId region, ResourceId resource) =>
        $"{owner}:{region}:{resource}";

    /// <summary>Quantity held, or zero if absent.</summary>
    public static decimal GetQuantity(
        EconomyState state,
        LegalEntityId owner,
        RegionId region,
        ResourceId resource)
    {
        var k = Key(owner, region, resource);
        return state.Holdings.TryGetValue(k, out var h) ? h.Quantity : 0m;
    }

    /// <summary>Upsert a holding; removes the slot when quantity is zero.</summary>
    public static EconomyState Upsert(
        EconomyState state,
        LegalEntityId owner,
        RegionId region,
        ResourceId resource,
        decimal quantity)
    {
        var holdings = new Dictionary<string, ResourceHolding>(state.Holdings);
        var k = Key(owner, region, resource);
        if (quantity <= 0m)
            holdings.Remove(k);
        else
            holdings[k] = new ResourceHolding(owner, region, resource, quantity);
        return state with { Holdings = holdings };
    }

    /// <summary>Increase quantity (creates slot if needed).</summary>
    public static EconomyState Credit(
        EconomyState state,
        LegalEntityId owner,
        RegionId region,
        ResourceId resource,
        decimal quantity)
    {
        if (quantity < 0m)
            throw new ArgumentOutOfRangeException(nameof(quantity));
        if (quantity == 0m)
            return state;
        var have = GetQuantity(state, owner, region, resource);
        return Upsert(state, owner, region, resource, have + quantity);
    }

    /// <summary>Decrease quantity; throws if insufficient.</summary>
    public static EconomyState Debit(
        EconomyState state,
        LegalEntityId owner,
        RegionId region,
        ResourceId resource,
        decimal quantity)
    {
        if (quantity < 0m)
            throw new ArgumentOutOfRangeException(nameof(quantity));
        if (quantity == 0m)
            return state;
        var have = GetQuantity(state, owner, region, resource);
        if (have + 1e-12m < quantity)
            throw new InvalidOperationException(
                $"Insufficient holding {resource} for {owner} in {region}: have {have}, need {quantity}.");
        return Upsert(state, owner, region, resource, have - quantity);
    }

    /// <summary>Transfer quantity between owners in the same region (trade settlement).</summary>
    public static EconomyState TransferOwnership(
        EconomyState state,
        LegalEntityId from,
        LegalEntityId to,
        RegionId region,
        ResourceId resource,
        decimal quantity)
    {
        state = Debit(state, from, region, resource, quantity);
        return Credit(state, to, region, resource, quantity);
    }
}
