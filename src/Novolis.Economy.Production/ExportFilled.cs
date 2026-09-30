using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Export filled into exogenous demand (inventory removed; cash credited).</summary>
public sealed record ExportFilled(
  SimulationHour Hour,
  FirmId FirmId,
  ProductId ProductId,
  Quantity Quantity,
  Money UnitPrice,
  Money Revenue) : IEconomyEvent;
