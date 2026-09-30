using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>A production batch was created.</summary>
public sealed record BatchProduced(
  SimulationHour Hour,
  FirmId FirmId,
  FacilityId FacilityId,
  ProductId ProductId,
  Quantity Quantity,
  Money UnitCost) : IEconomyEvent;
