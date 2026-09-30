using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Goods spoiled and were written off.</summary>
public sealed record InventorySpoiled(
  SimulationHour Hour,
  FirmId FirmId,
  ProductId ProductId,
  Quantity Quantity) : IEconomyEvent;
