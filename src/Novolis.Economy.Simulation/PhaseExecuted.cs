using Novolis.Economy;
using Novolis.Economy.Agents;
using Novolis.Economy.Simulation.Bounded;

namespace Novolis.Economy.Simulation;

/// <summary>Diagnostic event emitted by skeleton phases.</summary>
/// <param name="Hour">Hour when the phase ran.</param>
/// <param name="Phase">Phase that executed.</param>
public sealed record PhaseExecuted(SimulationHour Hour, SimulationPhaseOrder Phase) : IEconomyEvent;
