using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Event raised when a retail price changes.</summary>
public sealed record RetailPriceChanged(
  SimulationDate Date,
  FirmId FirmId,
  FacilityId FacilityId,
  ProductId ProductId,
  Money PreviousPrice,
  Money CurrentPrice) : IEconomyEvent;
