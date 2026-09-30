using System.Collections.Immutable;
using Novolis.Economy;
using Novolis.Economy.Abstractions;

namespace Novolis.Economy.Simulation;

/// <summary>Result of advancing the simulation.</summary>
/// <param name="HoursAdvanced">Hours successfully advanced.</param>
/// <param name="EventsEmitted">Events appended during the advance.</param>
/// <param name="FinalHash">State hash after the advance.</param>
public sealed record SimulationResult(
  long HoursAdvanced,
  int EventsEmitted,
  ulong FinalHash);
