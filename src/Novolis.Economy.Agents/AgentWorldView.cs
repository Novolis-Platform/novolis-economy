using Novolis.Economy.Accounting;
using Novolis.Economy.Finance;
using Novolis.Economy.Logistics;
using Novolis.Economy.Markets;
using Novolis.Economy.Population;
using Novolis.Economy.Production;

namespace Novolis.Economy.Agents;

/// <summary>
/// Read-only operational view supplied to an actor policy. It deliberately
/// exposes no simulation runner or state mutation methods.
/// </summary>
public interface IAgentWorldView
{
  InventoryStore Inventory { get; }

  IReadOnlyDictionary<FirmId, FirmLedger> Ledgers { get; }

  IReadOnlyList<ActiveShipment> Shipments { get; }

  IReadOnlyList<PlanShipment> PendingPlanShipments { get; }

  IReadOnlyList<PlanReposition> PendingPlanRepositions { get; }

  IReadOnlyList<CohortState> Cohorts { get; }

  IReadOnlyList<Loan> Loans { get; }

  IReadOnlyList<HubOrder> HubOrders { get; }

  IReadOnlyDictionary<TransportHubId, TransportHub> Hubs { get; }

  IReadOnlyDictionary<TransportCorridorId, TransportCorridor> Corridors { get; }

  IReadOnlyDictionary<VehicleClassId, VehicleClass> VehicleClasses { get; }

  Money WageRatePerHour { get; }

  Money TransportFuelUnitCost { get; }

  Money HouseholdComfortThresholdPerHousehold { get; }

  bool IsCreditFrozen(FirmId firmId);

  bool CanIssueShares(FirmId firmId);

  CohortState? FindCohortByHousehold(FirmId householdFirmId);

  Money ComfortFloor(CohortState cohort);

  bool IsAboveComfort(CohortState cohort);
}
