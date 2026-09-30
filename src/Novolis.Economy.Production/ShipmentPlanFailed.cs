using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Multi-leg plan could not be formed or departed.</summary>
public sealed record ShipmentPlanFailed(
  SimulationHour Hour,
  FirmId FirmId,
  ProductId ProductId,
  string Reason) : IEconomyEvent;
