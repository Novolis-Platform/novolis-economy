using System.Collections.Immutable;

namespace Novolis.Economy.Simulation;

/// <summary>Sorts and runs simulation phases in enum order.</summary>
public sealed class PhasePipeline
{
  private readonly ImmutableArray<ISimulationPhase> _phases;

  /// <summary>Creates a pipeline from phases (duplicates by order are rejected).</summary>
  public PhasePipeline(IEnumerable<ISimulationPhase> phases)
  {
    ArgumentNullException.ThrowIfNull(phases);
    var list = phases.OrderBy(p => p.Order).ToImmutableArray();
    if (list.Length == 0)
    {
      throw new ArgumentException("At least one phase is required.", nameof(phases));
    }

    var seen = new HashSet<SimulationPhaseOrder>();
    foreach (var phase in list)
    {
      if (!seen.Add(phase.Order))
      {
        throw new ArgumentException($"Duplicate phase order: {phase.Order}.", nameof(phases));
      }
    }

    _phases = list;
  }

  /// <summary>Phases in execution order.</summary>
  public ImmutableArray<ISimulationPhase> Phases => _phases;

  /// <summary>Executes all phases for the current tick.</summary>
  public async ValueTask<ImmutableArray<SimulationPhaseOrder>> ExecuteAsync(
    SimulationContext context,
    CancellationToken cancellationToken)
  {
    var executed = ImmutableArray.CreateBuilder<SimulationPhaseOrder>(_phases.Length);
    foreach (var phase in _phases)
    {
      cancellationToken.ThrowIfCancellationRequested();
      await phase.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
      executed.Add(phase.Order);
    }

    return executed.ToImmutable();
  }

  /// <summary>Creates the default twelve skeleton phases.</summary>
  public static PhasePipeline CreateDefault() => new(DefaultPhases.Create());
}

/// <summary>Factory for the twelve skeleton phases.</summary>
public static class DefaultPhases
{
  /// <summary>Creates one instance of each default phase.</summary>
  public static IReadOnlyList<ISimulationPhase> Create() =>
  [
    new Phases.ApplyDecisionsPhase(),
    new Phases.AllocateLaborPhase(),
    new Phases.AcquireInputsPhase(),
    new Phases.TransportInventoryPhase(),
    new Phases.RunProductionPhase(),
    new Phases.RestockRetailPhase(),
    new Phases.ResolveConsumerPurchasesPhase(),
    new Phases.SettleInvoicesAndWagesPhase(),
    new Phases.ApplyResearchProgressPhase(),
    new Phases.UpdateExpectationsPhase(),
    new Phases.CloseAccountingPeriodPhase(),
    new Phases.EmitObservationsPhase(),
  ];
}
