using Novolis.Economy.Core.Holdings;

namespace Novolis.Economy.Core.Transport;

/// <summary>Start / tick / complete resource transfers (SPEC §9).</summary>
public static class TransferEngine
{
    /// <summary>Lane key Origin→Destination.</summary>
    public static string LaneKey(RegionId origin, RegionId destination) => $"{origin}->{destination}";

    /// <summary>
    /// Debit origin holding and enqueue an in-flight transfer.
    /// Respects regional logistics capacity and lane capacity.
    /// Ownership is preserved unless a separate sale changes Owner.
    /// </summary>
    public static EconomyState StartTransfer(
        EconomyState state,
        LegalEntityId owner,
        ResourceId resource,
        decimal quantity,
        RegionId origin,
        RegionId destination)
    {
        if (quantity <= 0m)
            return state;
        if (origin.Equals(destination))
            throw new InvalidOperationException("Origin and destination must differ.");

        if (!state.Regions.TryGetValue(origin, out var originRegion))
            throw new InvalidOperationException($"Unknown origin region {origin}.");

        var laneKey = LaneKey(origin, destination);
        if (!state.Lanes.TryGetValue(laneKey, out var lane))
            throw new InvalidOperationException($"No lane {laneKey}.");

        var remainingLogistics = RegionCapacity.RemainingLogistics(state, originRegion);
        var qty = Math.Min(quantity, remainingLogistics);
        qty = Math.Min(qty, lane.CapacityPerPeriod);
        if (qty <= 0m)
            return state;

        state = HoldingLedger.Debit(state, owner, origin, resource, qty);
        var transfers = new List<ResourceTransfer>(state.Transfers)
        {
            new(owner, resource, qty, origin, destination, lane.TravelPeriods)
        };
        return state with { Transfers = transfers };
    }

    /// <summary>Decrement RemainingPeriods; complete arrivals by crediting destination holdings.</summary>
    public static EconomyState TickAndComplete(EconomyState state)
    {
        var next = new List<ResourceTransfer>();
        foreach (var t in state.Transfers)
        {
            var remaining = t.RemainingPeriods - 1;
            if (remaining <= 0)
            {
                state = HoldingLedger.Credit(state, t.Owner, t.Destination, t.ResourceId, t.Quantity);
            }
            else
            {
                next.Add(t with { RemainingPeriods = remaining });
            }
        }

        return state with { Transfers = next };
    }
}
