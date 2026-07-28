namespace Novolis.Economy.Logistics.Extensions;

/// <summary>Read-only logistics network insights.</summary>
public static class LogisticsNetworkExtensions
{
    public static LogisticsSnapshot Snapshot(
        this IEnumerable<ActiveShipment> shipments,
        IReadOnlyDictionary<TransportHubId, TransportHub> hubs,
        IReadOnlyDictionary<TransportCorridorId, TransportCorridor> corridors)
    {
        var list = shipments.ToList();
        var byPhase = Enum.GetValues<ShipmentPhase>()
            .ToDictionary(p => p, p => list.Count(s => s.Phase == p));

        var inFlight = list
            .Where(s => s.Phase is not (ShipmentPhase.Delivered or ShipmentPhase.Cancelled))
            .Sum(s => s.Quantity.Value);

        var tollExposure = Money.From(corridors.Values.Sum(c => c.Toll.Amount));

        var constrained = hubs.Values.Where(h => h.BerthCapacity > 0).ToList();
        var berthUtil = 0m;
        if (constrained.Count > 0)
        {
            // Ships waiting or dwelling at a hub count against that hub's berth capacity.
            var load = list.Count(s =>
                s.Phase is ShipmentPhase.WaitingBerth or ShipmentPhase.Loading or ShipmentPhase.Unloading);
            var capacity = constrained.Sum(h => h.BerthCapacity);
            berthUtil = capacity <= 0 ? 0m : (decimal)load / capacity;
        }

        return new LogisticsSnapshot(
            HubCount: hubs.Count,
            CorridorCount: corridors.Count,
            ShipmentCount: list.Count,
            ShipmentsByPhase: byPhase,
            CargoQuantityInFlight: inFlight,
            CorridorTollExposure: tollExposure,
            BerthConstrainedHubs: constrained.Count,
            AverageBerthUtilization: berthUtil);
    }
}
