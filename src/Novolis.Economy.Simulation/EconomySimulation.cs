using Novolis.Economy;

namespace Novolis.Economy.Simulation;

/// <summary>Default deterministic economy simulation.</summary>
public sealed class EconomySimulation : IEconomySimulation
{
  private readonly PhasePipeline _pipeline;
  private readonly DeterministicRandom _random;
  private readonly SimulationContext _context;

  /// <summary>Creates a simulation with an empty world.</summary>
  public EconomySimulation(ulong seed)
    : this(seed, new EconomyWorld(), PhasePipeline.CreateDefault())
  {
  }

  /// <summary>Creates a simulation with a prepared world.</summary>
  public EconomySimulation(ulong seed, EconomyWorld world)
    : this(seed, world, PhasePipeline.CreateDefault())
  {
  }

  /// <summary>Creates a simulation with a custom pipeline (tests).</summary>
  public EconomySimulation(ulong seed, PhasePipeline pipeline)
    : this(seed, new EconomyWorld(), pipeline)
  {
  }

  /// <summary>Creates a simulation with world and pipeline.</summary>
  public EconomySimulation(ulong seed, EconomyWorld world, PhasePipeline pipeline)
  {
    ArgumentNullException.ThrowIfNull(world);
    ArgumentNullException.ThrowIfNull(pipeline);
    State = new SimulationState(seed, world);
    _pipeline = pipeline;
    _random = new DeterministicRandom(seed);
    _context = new SimulationContext(State, _random, State.Entropy);
    world.CoreState = world.CoreState with { SimulationSeed = seed };
    CoreInventoryBridge.ReconcileAll(world);
  }

  /// <inheritdoc />
  public SimulationState State { get; }

  /// <summary>Named deterministic entropy for agents and phases.</summary>
  public SimulationEntropy Entropy => State.Entropy;

  /// <summary>
  /// When true, hourly ticks skip non-essential economy phases (see <see cref="SimulationContext.ThroughputMode"/>).
  /// </summary>
  public bool ThroughputMode
  {
    get => _context.ThroughputMode;
    set => _context.ThroughputMode = value;
  }

  /// <inheritdoc />
  public void Enqueue(IEconomyCommand command) => State.EnqueueCommand(command);

  /// <inheritdoc />
  public async ValueTask<SimulationResult> AdvanceAsync(
    SimulationDuration duration,
    CancellationToken cancellationToken = default)
    => await AdvanceAsync(duration, computeFinalHash: true, cancellationToken).ConfigureAwait(false);

  /// <summary>
  /// Advances the simulation. When <paramref name="computeFinalHash"/> is false, skips the
  /// expensive world fingerprint (callers that need <see cref="SimulationState.Hash"/> can read it later).
  /// </summary>
  public async ValueTask<SimulationResult> AdvanceAsync(
    SimulationDuration duration,
    bool computeFinalHash,
    CancellationToken cancellationToken = default)
  {
    ArgumentOutOfRangeException.ThrowIfNegative(duration.Hours);
    var eventsBefore = State.Events.Count;
    for (var i = 0L; i < duration.Hours; i++)
    {
      cancellationToken.ThrowIfCancellationRequested();
      CoreCashBridge.ReconcileAll(State.World);
      CoreInventoryBridge.ReconcileAll(State.World);
      State.BeginTick();
      var phases = await _pipeline.ExecuteAsync(_context, cancellationToken).ConfigureAwait(false);
      State.CompleteTick(phases, _random.State);
    }

    return new SimulationResult(
      duration.Hours,
      State.Events.Count - eventsBefore,
      computeFinalHash ? State.Hash : 0UL);
  }
}

/// <summary>Headless economic simulation entry point.</summary>
public interface IEconomySimulation
{
  /// <summary>Current mutable state.</summary>
  SimulationState State { get; }

  /// <summary>Enqueues a command for a future tick.</summary>
  void Enqueue(IEconomyCommand command);

  /// <summary>Advances the simulation by the given duration.</summary>
  ValueTask<SimulationResult> AdvanceAsync(
    SimulationDuration duration,
    CancellationToken cancellationToken = default);
}
