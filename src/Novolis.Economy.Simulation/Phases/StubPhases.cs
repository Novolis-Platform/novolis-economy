using Novolis.Economy;

namespace Novolis.Economy.Simulation.Phases;

/// <summary>Base skeleton phase that records a diagnostic event.</summary>
public abstract class RecordingPhase : ISimulationPhase
{
  /// <inheritdoc />
  public abstract SimulationPhaseOrder Order { get; }

  /// <inheritdoc />
  public virtual ValueTask ExecuteAsync(SimulationContext context, CancellationToken cancellationToken)
  {
    context.State.AppendEvent(new PhaseExecuted(context.State.Clock, Order));
    return ValueTask.CompletedTask;
  }
}

/// <summary>Applies queued commands (skeleton: dequeue only).</summary>
public sealed class ApplyDecisionsPhase : RecordingPhase
{
  /// <inheritdoc />
  public override SimulationPhaseOrder Order => SimulationPhaseOrder.ApplyDecisions;

  /// <inheritdoc />
  public override ValueTask ExecuteAsync(SimulationContext context, CancellationToken cancellationToken)
  {
    _ = context.State.DequeueCommands();
    return base.ExecuteAsync(context, cancellationToken);
  }
}

/// <summary>Labor allocation stub.</summary>
public sealed class AllocateLaborPhase : RecordingPhase
{
  /// <inheritdoc />
  public override SimulationPhaseOrder Order => SimulationPhaseOrder.AllocateLabor;
}

/// <summary>Input acquisition stub.</summary>
public sealed class AcquireInputsPhase : RecordingPhase
{
  /// <inheritdoc />
  public override SimulationPhaseOrder Order => SimulationPhaseOrder.AcquireInputs;
}

/// <summary>Transport stub.</summary>
public sealed class TransportInventoryPhase : RecordingPhase
{
  /// <inheritdoc />
  public override SimulationPhaseOrder Order => SimulationPhaseOrder.TransportInventory;
}

/// <summary>Production stub.</summary>
public sealed class RunProductionPhase : RecordingPhase
{
  /// <inheritdoc />
  public override SimulationPhaseOrder Order => SimulationPhaseOrder.RunProduction;
}

/// <summary>Retail restock stub.</summary>
public sealed class RestockRetailPhase : RecordingPhase
{
  /// <inheritdoc />
  public override SimulationPhaseOrder Order => SimulationPhaseOrder.RestockRetail;
}

/// <summary>Consumer purchase stub.</summary>
public sealed class ResolveConsumerPurchasesPhase : RecordingPhase
{
  /// <inheritdoc />
  public override SimulationPhaseOrder Order => SimulationPhaseOrder.ResolveConsumerPurchases;
}

/// <summary>Settlement stub.</summary>
public sealed class SettleInvoicesAndWagesPhase : RecordingPhase
{
  /// <inheritdoc />
  public override SimulationPhaseOrder Order => SimulationPhaseOrder.SettleInvoicesAndWages;
}

/// <summary>Research stub.</summary>
public sealed class ApplyResearchProgressPhase : RecordingPhase
{
  /// <inheritdoc />
  public override SimulationPhaseOrder Order => SimulationPhaseOrder.ApplyResearchProgress;
}

/// <summary>Expectations stub.</summary>
public sealed class UpdateExpectationsPhase : RecordingPhase
{
  /// <inheritdoc />
  public override SimulationPhaseOrder Order => SimulationPhaseOrder.UpdateExpectations;
}

/// <summary>Accounting close stub.</summary>
public sealed class CloseAccountingPeriodPhase : RecordingPhase
{
  /// <inheritdoc />
  public override SimulationPhaseOrder Order => SimulationPhaseOrder.CloseAccountingPeriod;
}

/// <summary>Observation emission stub.</summary>
public sealed class EmitObservationsPhase : RecordingPhase
{
  /// <inheritdoc />
  public override SimulationPhaseOrder Order => SimulationPhaseOrder.EmitObservations;
}
