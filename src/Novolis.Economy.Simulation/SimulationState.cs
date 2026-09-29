using System.Collections.Immutable;
using Novolis.Economy;
using Novolis.Economy.Abstractions;

namespace Novolis.Economy.Simulation;

/// <summary>Mutable runtime state for the economic simulation.</summary>
public sealed class SimulationState
{
  private readonly List<IEconomyCommand> _pendingCommands = [];
  private readonly List<IEconomyEvent> _events = [];
  private readonly List<AgentDecisionTrace> _agentDecisionTraces = [];
  private readonly List<SimulationPhaseOrder> _lastTickPhases = [];
  private ulong _lastRngState;
  private ulong _cachedWorldFingerprint;
  private ulong _hash;
  private bool _worldFingerprintDirty;
  private bool _hashDirty;

  /// <summary>Creates state at epoch with the given seed and an empty world.</summary>
  public SimulationState(ulong seed)
    : this(seed, new EconomyWorld())
  {
  }

  /// <summary>Creates state at epoch with the given seed and world.</summary>
  public SimulationState(
    ulong seed,
    EconomyWorld world,
    EconomicModelIdentity? modelIdentity = null)
  {
    ArgumentNullException.ThrowIfNull(world);
    Seed = seed;
    World = world;
    ModelIdentity = modelIdentity
      ?? new EconomicModelIdentity(
        "OperationalHost",
        world.Specification.Version);
    World.CoreState = World.CoreState with { SimulationSeed = seed };
    Entropy = new SimulationEntropy(seed);
    Clock = SimulationHour.Epoch;
    _lastRngState = seed == 0 ? 0x9E3779B97F4A7C15UL : seed;
    _cachedWorldFingerprint = world.Fingerprint();
    var specificationHash = SimulationRunManifest.HashSpecification(world.Specification);
    Manifest = new SimulationRunManifest(
      world.Specification.Version,
      seed,
      _cachedWorldFingerprint,
      world.Policy.PeriodHours,
      typeof(SimulationState).Assembly.GetName().Version?.ToString() ?? "unknown",
      specificationHash,
      SimulationRunManifest.HashScenario(
        seed,
        _cachedWorldFingerprint,
        specificationHash,
        ModelIdentity.Id,
        ModelIdentity.Version,
        string.Empty),
      ModelIdentity.Id,
      string.Empty);
    RecomputeHash();
  }

  /// <summary>Initial RNG seed.</summary>
  public ulong Seed { get; }

  /// <summary>Complete model selected for this run.</summary>
  public EconomicModelIdentity ModelIdentity { get; }

  /// <summary>Named deterministic entropy streams for this run.</summary>
  public SimulationEntropy Entropy { get; }

  /// <summary>Reproducibility metadata captured at run creation.</summary>
  public SimulationRunManifest Manifest { get; }

  /// <summary>Economic world.</summary>
  public EconomyWorld World { get; }

  /// <summary>Current simulation hour.</summary>
  public SimulationHour Clock { get; private set; }

  /// <summary>Commands waiting to be applied.</summary>
  public IReadOnlyList<IEconomyCommand> PendingCommands => _pendingCommands;

  /// <summary>Diagnostic and domain events accumulated so far.</summary>
  public IReadOnlyList<IEconomyEvent> Events => _events;

  /// <summary>Deterministic actor decisions emitted during this run.</summary>
  public IReadOnlyList<AgentDecisionTrace> AgentDecisionTraces =>
    _agentDecisionTraces;

  /// <summary>Records one actor decision without mutating economic authority.</summary>
  public void AppendAgentDecision(AgentDecisionTrace trace)
  {
    ArgumentNullException.ThrowIfNull(trace);
    _agentDecisionTraces.Add(trace);
    _hashDirty = true;
  }

  /// <summary>Phases that ran during the most recent tick (for tests).</summary>
  public IReadOnlyList<SimulationPhaseOrder> LastTickPhases => _lastTickPhases;

  /// <summary>Deterministic fingerprint of clock, world, and RNG (lazy).</summary>
  public ulong Hash
  {
    get
    {
      EnsureHash();
      return _hash;
    }
  }

  /// <summary>Enqueues a command for the next ApplyDecisions phase.</summary>
  public void EnqueueCommand(IEconomyCommand command)
  {
    ArgumentNullException.ThrowIfNull(command);
    _pendingCommands.Add(command);
    // World is unchanged until the next tick — reuse cached fingerprint.
    _hashDirty = true;
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
    // Defer World.Fingerprint — dominant cost of long free-runs; Hash getter refreshes.
    _worldFingerprintDirty = true;
    _hashDirty = true;
  }

  /// <summary>Begins a tick; clears last-tick phase list.</summary>
  internal void BeginTick()
  {
    _lastTickPhases.Clear();
  }

  private void EnsureHash()
  {
    if (!_hashDirty && !_worldFingerprintDirty)
    {
      return;
    }

    if (_worldFingerprintDirty)
    {
      _cachedWorldFingerprint = World.Fingerprint();
      _worldFingerprintDirty = false;
    }

    RecomputeHash();
    _hashDirty = false;
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
    hash = (hash ^ _cachedWorldFingerprint) * prime;
    hash = (hash ^ (ulong)_agentDecisionTraces.Count) * prime;
    foreach (var trace in _agentDecisionTraces)
    {
      hash = (hash ^ (ulong)trace.Hour.HourIndex) * prime;
      hash = (hash ^ HashGuid(trace.FirmId.Value)) * prime;
      hash = (hash ^ HashString(trace.AgentType)) * prime;
      hash = (hash ^ HashString(trace.Decision)) * prime;
      hash = (hash ^ trace.RandomState) * prime;
    }
    _hash = hash;
  }

  private static ulong HashGuid(Guid value)
  {
    const ulong offset = 14695981039346656037UL;
    const ulong prime = 1099511628211UL;
    var hash = offset;
    foreach (var b in value.ToByteArray())
    {
      hash = (hash ^ b) * prime;
    }

    return hash;
  }

  private static ulong HashString(string value)
  {
    const ulong offset = 14695981039346656037UL;
    const ulong prime = 1099511628211UL;
    var hash = offset;
    foreach (var b in System.Text.Encoding.UTF8.GetBytes(value))
    {
      hash = (hash ^ b) * prime;
    }

    return hash;
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
