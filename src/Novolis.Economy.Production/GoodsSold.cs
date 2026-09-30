using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Goods sold to a consumer cohort.</summary>
public sealed record GoodsSold(
  SimulationHour Hour,
  FirmId FirmId,
  FacilityId FacilityId,
  ConsumerCohortId CohortId,
  ProductId ProductId,
  Quantity Quantity,
  Money UnitPrice,
  Money Revenue) : IEconomyEvent;
