using Novolis.Economy.Core.Transactions;
using Novolis.Economy.Primitives;

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
        => PositionLedger.GetQuantity(
            state,
            EconomicIdentity.For(owner),
            region,
            state.AssetFor(resource));

    /// <summary>Upsert a holding; removes the slot when quantity is zero.</summary>
    public static EconomyState Upsert(
        EconomyState state,
        LegalEntityId owner,
        RegionId region,
        ResourceId resource,
        decimal quantity)
    {
        if (quantity < 0m)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        var current = GetQuantity(state, owner, region, resource);
        return PositionLedger.ApplyResource(state, owner, region, resource, quantity - current);
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
        if (quantity < 0m)
            throw new ArgumentOutOfRangeException(nameof(quantity));
        if (quantity == 0m)
            return state;

        var asset = state.AssetFor(resource);
        var fromOwner = EconomicIdentity.For(from);
        var toOwner = EconomicIdentity.For(to);
        var fromQuantity = PositionLedger.GetQuantity(state, fromOwner, region, asset);
        if (fromQuantity + 1e-12m < quantity)
            throw new InvalidOperationException(
                $"Insufficient holding {resource} for {from} in {region}: " +
                $"have {fromQuantity}, need {quantity}.");
        var toQuantity = PositionLedger.GetQuantity(state, toOwner, region, asset);

        var next = EconomicTransactionEngine.Apply(
            state,
            EconomicTransaction.Create(
                state,
                [
                    new PositionChange(fromOwner, asset, -quantity, region),
                    new PositionChange(toOwner, asset, quantity, region)
                ],
                "ownership-transfer"));

        next = PositionLedger.UpdateLegacyResourceProjection(
            next,
            from,
            region,
            resource,
            fromQuantity - quantity);
        return PositionLedger.UpdateLegacyResourceProjection(
            next,
            to,
            region,
            resource,
            toQuantity + quantity);
    }
}
