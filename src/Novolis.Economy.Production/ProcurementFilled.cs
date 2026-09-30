using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Procurement filled from exogenous supply.</summary>
public sealed record ProcurementFilled(
  SimulationHour Hour,
  FirmId FirmId,
  ProductId ProductId,
  Quantity Quantity,
  Money UnitPrice) : IEconomyEvent;
