namespace Novolis.Economy.Logistics.Extensions;

/// <summary>Logistics network / in-flight cargo snapshot.</summary>
public sealed record LogisticsSnapshot(
    int HubCount,
    int CorridorCount,
    int ShipmentCount,
    IReadOnlyDictionary<ShipmentPhase, int> ShipmentsByPhase,
    decimal CargoQuantityInFlight,
    Money CorridorTollExposure,
    int BerthConstrainedHubs,
    decimal AverageBerthUtilization);
