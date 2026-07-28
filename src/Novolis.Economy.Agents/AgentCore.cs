using Novolis.Economy;
using Novolis.Economy.Logistics;
using Novolis.Economy.Simulation;

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

/// <summary>Read/write handle passed to agents each tick.</summary>
public sealed class AgentContext
{
  /// <summary>Creates a context bound to a simulation and RNG stream.</summary>
  public AgentContext(EconomySimulation simulation, DeterministicRandom rng)
  {
    Simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
    Rng = rng ?? throw new ArgumentNullException(nameof(rng));
  }

  /// <summary>Host simulation.</summary>
  public EconomySimulation Simulation { get; }

  /// <summary>World snapshot.</summary>
  public EconomyWorld World => Simulation.State.World;

  /// <summary>Current clock (pre-advance).</summary>
  public SimulationHour Clock => Simulation.State.Clock;

  /// <summary>Deterministic jitter source for this agent / pulse.</summary>
  public DeterministicRandom Rng { get; }

  /// <summary>Enqueue a command for the next ApplyDecisions phase.</summary>
  public void Enqueue(IEconomyCommand command) => Simulation.Enqueue(command);
}

/// <summary>Runs agents in order; caller advances the simulation.</summary>
public static class AgentScheduler
{
  /// <summary>Ticks each agent once.</summary>
  public static void TickAll(IEnumerable<IEconomicAgent> agents, AgentContext context)
  {
    ArgumentNullException.ThrowIfNull(agents);
    ArgumentNullException.ThrowIfNull(context);
    foreach (var agent in agents)
    {
      agent.Tick(context);
    }
  }
}

/// <summary>Inventory location + optional facility / hub binding for agent policies.</summary>
public sealed record AgentSite(
  InventoryLocationId LocationId,
  FacilityId? FacilityId = null,
  TransportHubId? HubId = null,
  string Name = "");
