using Novolis.Economy.Agents;

namespace Novolis.Economy.Simulation;

/// <summary>Read-side trace of a rules-based actor pulse.</summary>
public sealed record AgentDecisionTrace(
  SimulationHour Hour,
  FirmId FirmId,
  string AgentType,
  string Decision,
  ulong RandomState);
