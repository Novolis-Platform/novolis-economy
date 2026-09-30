using Novolis.Economy;
using Novolis.Economy.Logistics;

using System.Reflection;
using Novolis.Economy.Abstractions;
using Novolis.Economy.Production;

namespace Novolis.Economy.Agents;

/// <summary>
/// Economic decision-maker for a firm: observes the world and enqueues commands.
/// Heuristics + <see cref="DeterministicRandom"/> only — not ML.
/// </summary>
public interface IEconomicAgent
{
  /// <summary>Firm this agent operates.</summary>
  FirmId FirmId { get; }

  /// <summary>Short status line for dashboards / reports.</summary>
  string LastDecision { get; }

  /// <summary>Enqueue decisions for the current hour (before <c>AdvanceAsync</c>).</summary>
  void Tick(AgentContext context);
}
