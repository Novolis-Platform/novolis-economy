using System.Collections.Immutable;
using Novolis.Economy;

namespace Novolis.Economy.Simulation;

/// <summary>Mutable runtime state for the economic simulation.</summary>
public sealed class SimulationState
{
  private readonly List<IEconomyCommand> _pendingCommands = [];
  private readonly List<IEconomyEvent> _events = [];
  private readonly List<SimulationPhaseOrder> _lastTickPhases = [];
  private ulong _lastRngState;

  /// <summary>Creates state at epoch with the given seed and an empty world.</summary>
  public SimulationState(ulong seed)
    : this(seed, new EconomyWorld())
  {
  }

  /// <summary>Creates state at epoch with the given seed and world.</summary>
  public SimulationState(ulong seed, EconomyWorld world)
  {
    ArgumentNullException.ThrowIfNull(world);
    Seed = seed;
    World = world;
    Clock = SimulationHour.Epoch;
    _lastRngState = seed == 0 ? 0x9E3779B97F4A7C15UL : seed;
    RecomputeHash();
  }

  /// <summary>Initial RNG seed.</summary>
  public ulong Seed { get; }

  /// <summary>Economic world.</summary>
  public EconomyWorld World { get; }

  /// <summary>Current simulation hour.</summary>
  public SimulationHour Clock { get; private set; }

  /// <summary>Commands waiting to be applied.</summary>
  public IReadOnlyList<IEconomyCommand> PendingCommands => _pendingCommands;

  /// <summary>Diagnostic and domain events accumulated so far.</summary>
  public IReadOnlyList<IEconomyEvent> Events => _events;

  /// <summary>Phases that ran during the most recent tick (for tests).</summary>
  public IReadOnlyList<SimulationPhaseOrder> LastTickPhases => _lastTickPhases;

  /// <summary>Deterministic fingerprint of clock, world, and RNG.</summary>
  public ulong Hash { get; private set; }

  /// <summary>Enqueues a command for the next ApplyDecisions phase.</summary>
  public void EnqueueCommand(IEconomyCommand command)
  {
    ArgumentNullException.ThrowIfNull(command);
    _pendingCommands.Add(command);
    RecomputeHash();
  }

  /// <summary>Dequeues all pending commands (used by ApplyDecisions).</summary>
  public ImmutableArray<IEconomyCommand> DequeueCommands()
  {
    var snapshot = _pendingCommands.ToImmutableArray();
    _pendingCommands.Clear();
    return snapshot;
  }

  /// <summary>Appends an event to the buffer.</summary>
  public void AppendEvent(IEconomyEvent economyEvent)
  {
    ArgumentNullException.ThrowIfNull(economyEvent);
    _events.Add(economyEvent);
  }

  /// <summary>Records phases executed this tick and advances the clock by one hour.</summary>
  internal void CompleteTick(IReadOnlyList<SimulationPhaseOrder> phases, ulong rngState)
  {
    _lastTickPhases.Clear();
    _lastTickPhases.AddRange(phases);
    Clock = Clock.AddHours(1);
    _lastRngState = rngState;
    RecomputeHash();
  }

  /// <summary>Begins a tick; clears last-tick phase list.</summary>
  internal void BeginTick()
  {
    _lastTickPhases.Clear();
  }

  private void RecomputeHash()
  {
    const ulong offset = 14695981039346656037UL;
    const ulong prime = 1099511628211UL;
    var hash = offset;
    hash = (hash ^ Seed) * prime;
    hash = (hash ^ (ulong)Clock.HourIndex) * prime;
    hash = (hash ^ (ulong)_pendingCommands.Count) * prime;
    hash = (hash ^ (ulong)_events.Count) * prime;
    hash = (hash ^ _lastRngState) * prime;
    hash = (hash ^ World.Fingerprint()) * prime;
    Hash = hash;
  }
}

/// <summary>Result of advancing the simulation.</summary>
/// <param name="HoursAdvanced">Hours successfully advanced.</param>
/// <param name="EventsEmitted">Events appended during the advance.</param>
/// <param name="FinalHash">State hash after the advance.</param>
public sealed record SimulationResult(
  long HoursAdvanced,
  int EventsEmitted,
  ulong FinalHash);
