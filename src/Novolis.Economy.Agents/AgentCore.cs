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

/// <summary>Read/write handle passed to agents each tick.</summary>
public sealed class AgentContext
{
  private readonly Action<IEconomyCommand> _enqueue;

  /// <summary>Creates a context from a read-only world view and command sink.</summary>
  public AgentContext(
    IAgentWorldView world,
    SimulationHour clock,
    IAgentRandom rng,
    Action<IEconomyCommand> enqueue,
    object? simulation = null)
  {
    World = world ?? throw new ArgumentNullException(nameof(world));
    Clock = clock;
    Rng = rng ?? throw new ArgumentNullException(nameof(rng));
    _enqueue = enqueue ?? throw new ArgumentNullException(nameof(enqueue));
    Simulation = simulation;
  }

  /// <summary>
  /// Compatibility constructor for existing hosts. The dependency remains
  /// late-bound so the Agents assembly does not reference Simulation.
  /// </summary>
  public AgentContext(object simulation, IAgentRandom rng)
  {
    ArgumentNullException.ThrowIfNull(simulation);
    Rng = rng ?? throw new ArgumentNullException(nameof(rng));
    Simulation = simulation;
    var state = GetProperty(simulation, "State");
    World = (IAgentWorldView)GetProperty(state, "World");
    Clock = (SimulationHour)GetProperty(state, "Clock");
    _enqueue = command => Invoke(simulation, "Enqueue", command);
  }

  /// <summary>Read-only operational world view.</summary>
  public IAgentWorldView World { get; }

  /// <summary>
  /// Optional product host surface for actors. It is late-bound so Agents
  /// remains independent of the Simulation assembly.
  /// </summary>
  public dynamic? Simulation { get; }

  /// <summary>Current clock (pre-advance).</summary>
  public SimulationHour Clock { get; }

  /// <summary>Deterministic jitter source for this agent / pulse.</summary>
  public IAgentRandom Rng { get; }

  /// <summary>Enqueue a command for the next ApplyDecisions phase.</summary>
  public void Enqueue(IEconomyCommand command) => _enqueue(command);

  private static object GetProperty(object target, string name) =>
    target.GetType().GetProperty(
      name,
      BindingFlags.Instance | BindingFlags.Public)?.GetValue(target)
    ?? throw new InvalidOperationException(
      $"Agent host does not expose a readable {name} property.");

  private static void Invoke(object target, string name, object argument)
  {
    var method = target.GetType().GetMethod(
      name,
      BindingFlags.Instance | BindingFlags.Public,
      binder: null,
      types: [argument.GetType()],
      modifiers: null);
    if (method is null)
      throw new InvalidOperationException($"Agent host does not expose {name}.");
    method.Invoke(target, [argument]);
  }
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
