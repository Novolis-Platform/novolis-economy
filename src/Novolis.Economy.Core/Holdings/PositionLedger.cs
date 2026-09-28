using Novolis.Economy.Primitives;
using Novolis.Economy.Core.Transactions;

namespace Novolis.Economy.Core.Holdings;

/// <summary>
/// Authoritative owner × asset × region position operations.
/// </summary>
public static class PositionLedger
{
    /// <summary>Stable key for one economic position slot.</summary>
    public static string Key(EconomicEntityId owner, RegionId? region, EconomicAssetId asset) =>
        $"{owner}:{region?.ToString() ?? "-"}:{asset}";

    /// <summary>Returns a materialized view of authoritative positions.</summary>
    public static IReadOnlyDictionary<string, EconomicPosition> Snapshot(EconomyState state)
    {
        var positions = new Dictionary<string, EconomicPosition>(state.PositionState);
        foreach (var holding in state.Holdings.Values)
        {
            if (holding.Quantity < 0m)
                continue;

            var position = new EconomicPosition(
                EconomicIdentity.For(holding.Owner),
                state.AssetFor(holding.ResourceId),
                holding.Quantity,
                holding.RegionId);
            positions.TryAdd(Key(position.Owner, position.Region, position.Asset), position);
        }

        return positions;
    }

    /// <summary>
    /// Projects resource-backed positions for legacy algorithms that still
    /// need resource classifications and posted-price keys.
    /// </summary>
    public static IReadOnlyList<ResourceHolding> ResourceView(EconomyState state)
    {
        var resourcesByAsset = state.Resources.Values
            .GroupBy(resource => state.AssetFor(resource.Id))
            .ToDictionary(group => group.Key, group => group.First().Id);

        return Snapshot(state)
            .Values
            .Where(position => position.Region is not null &&
                               resourcesByAsset.ContainsKey(position.Asset))
            .Select(position => new ResourceHolding(
                EconomicIdentity.ToLegalEntityId(position.Owner),
                position.Region!.Value,
                resourcesByAsset[position.Asset],
                position.Quantity))
            .ToList();
    }

    /// <summary>Returns a position quantity, or zero if the slot is absent.</summary>
    public static decimal GetQuantity(
        EconomyState state,
        EconomicEntityId owner,
        RegionId? region,
        EconomicAssetId asset)
    {
        var positions = Snapshot(state);
        return positions.TryGetValue(Key(owner, region, asset), out var position)
            ? position.Quantity
            : 0m;
    }

    /// <summary>Apply a signed delta while preserving non-negative stored positions.</summary>
    public static EconomyState Apply(
        EconomyState state,
        EconomicEntityId owner,
        RegionId? region,
        EconomicAssetId asset,
        decimal delta)
    {
        if (delta == 0m)
            return state;

        var positions = new Dictionary<string, EconomicPosition>(Snapshot(state));
        var key = Key(owner, region, asset);
        var current = positions.TryGetValue(key, out var existing) ? existing.Quantity : 0m;
        var next = current + delta;
        if (next < -1e-12m)
            throw new InvalidOperationException(
                $"Insufficient position quantity for owner {owner}, asset {asset}, region {region}: " +
                $"requested delta {delta}, available {current}.");

        if (next <= 1e-12m)
            positions.Remove(key);
        else
            positions[key] = new EconomicPosition(owner, asset, next, region);

        var nextState = state with { Positions = positions };
        if (region is not null)
        {
            foreach (var resource in state.Resources.Values.Where(resource => state.AssetFor(resource.Id).Equals(asset)))
            {
                nextState = UpdateLegacyResourceProjection(
                    nextState,
                    EconomicIdentity.ToLegalEntityId(owner),
                    region.Value,
                    resource.Id,
                    Math.Max(0m, next));
            }
        }

        return nextState;
    }

    /// <summary>Apply a resource delta and update the legacy holding projection.</summary>
    public static EconomyState ApplyResource(
        EconomyState state,
        LegalEntityId owner,
        RegionId region,
        ResourceId resource,
        decimal delta)
    {
        var next = EconomicTransactionEngine.Apply(
            state,
            new EconomicTransaction(
                TransactionId.From(Guid.Empty),
                [
                    new PositionChange(
                        EconomicIdentity.For(owner),
                        state.AssetFor(resource),
                        delta,
                        region)
                ],
                "resource-position-change"));

        var current = PositionLedger.GetQuantity(
            state,
            EconomicIdentity.For(owner),
            region,
            state.AssetFor(resource));
        var quantity = current + delta;
        if (quantity < -1e-12m)
            throw new InvalidOperationException(
                $"Insufficient holding quantity for {owner}/{region}/{resource}: " +
                $"requested delta {delta}, available {current}.");

        return UpdateLegacyResourceProjection(next, owner, region, resource, quantity);
    }

    /// <summary>Update the one-release ResourceHolding compatibility view.</summary>
    public static EconomyState UpdateLegacyResourceProjection(
        EconomyState state,
        LegalEntityId owner,
        RegionId region,
        ResourceId resource,
        decimal quantity)
    {
        var holdings = new Dictionary<string, ResourceHolding>(state.Holdings);
        var key = HoldingLedger.Key(owner, region, resource);
        if (quantity <= 1e-12m)
            holdings.Remove(key);
        else
            holdings[key] = new ResourceHolding(owner, region, resource, quantity);

        return state with { Holdings = holdings };
    }
}
