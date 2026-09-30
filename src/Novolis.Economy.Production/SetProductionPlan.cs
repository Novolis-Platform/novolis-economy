using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Sets hourly production rate for a product at a facility.</summary>
public sealed record SetProductionPlan(
  FirmId FirmId,
  FacilityId FacilityId,
  ProductId ProductId,
  Quantity RatePerHour) : IEconomyCommand;
