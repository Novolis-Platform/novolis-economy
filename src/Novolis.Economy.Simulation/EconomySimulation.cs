using Novolis.Economy;

namespace Novolis.Economy.Simulation;

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

/// <summary>Default deterministic economy simulation.</summary>
public sealed class EconomySimulation : IEconomySimulation
{
  private readonly PhasePipeline _pipeline;
  private readonly DeterministicRandom _random;
  private readonly SimulationContext _context;

  /// <summary>Creates a simulation with the default phase pipeline.</summary>
  public EconomySimulation(ulong seed)
    : this(seed, PhasePipeline.CreateDefault())
  {
  }

  /// <summary>Creates a simulation with a custom pipeline (tests).</summary>
  public EconomySimulation(ulong seed, PhasePipeline pipeline)
  {
    ArgumentNullException.ThrowIfNull(pipeline);
    State = new SimulationState(seed);
    _pipeline = pipeline;
    _random = new DeterministicRandom(seed);
    _context = new SimulationContext(State, _random);
  }

  /// <inheritdoc />
  public SimulationState State { get; }

  /// <inheritdoc />
  public void Enqueue(IEconomyCommand command) => State.EnqueueCommand(command);

  /// <inheritdoc />
  public async ValueTask<SimulationResult> AdvanceAsync(
    SimulationDuration duration,
    CancellationToken cancellationToken = default)
  {
    ArgumentOutOfRangeException.ThrowIfNegative(duration.Hours);
    var eventsBefore = State.Events.Count;
    for (var i = 0L; i < duration.Hours; i++)
    {
      cancellationToken.ThrowIfCancellationRequested();
      State.BeginTick();
      var phases = await _pipeline.ExecuteAsync(_context, cancellationToken).ConfigureAwait(false);
      State.CompleteTick(phases, _random.State);
    }

    return new SimulationResult(
      duration.Hours,
      State.Events.Count - eventsBefore,
      State.Hash);
  }
}
