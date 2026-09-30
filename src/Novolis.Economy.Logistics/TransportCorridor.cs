using System.Collections.Immutable;
using Novolis.Economy;

namespace Novolis.Economy.Logistics;

/// <summary>Directed corridor between hubs.</summary>
/// <param name="Id">Corridor id.</param>
/// <param name="From">Origin hub.</param>
/// <param name="To">Destination hub.</param>
/// <param name="TransitHours">Hours underway.</param>
/// <param name="MaxCargo">Max cargo quantity on this leg.</param>
/// <param name="Difficulty">Unitless leg difficulty; fuel burn scales with TransitHours * Difficulty.</param>
/// <param name="Toll">Money toll charged on departure into this corridor.</param>
public sealed record TransportCorridor(
  TransportCorridorId Id,
  TransportHubId From,
  TransportHubId To,
  long TransitHours,
  Quantity MaxCargo,
  decimal Difficulty,
  Money Toll);
