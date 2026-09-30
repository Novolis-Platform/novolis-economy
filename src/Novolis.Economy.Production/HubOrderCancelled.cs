using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Hub order removed from the book.</summary>
public sealed record HubOrderCancelled(
  SimulationHour Hour,
  Guid OrderId) : IEconomyEvent;
