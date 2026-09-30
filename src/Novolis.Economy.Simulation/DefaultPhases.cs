using System.Collections.Immutable;

namespace Novolis.Economy.Simulation;

/// <summary>Factory for the twelve skeleton phases.</summary>
public static class DefaultPhases
{
  /// <summary>Creates one instance of each default phase.</summary>
  public static IReadOnlyList<ISimulationPhase> Create() =>
  [
    new Phases.ApplyDecisionsPhase(),
    new Phases.AllocateLaborPhase(),
    new Phases.MatchHubOrdersPhase(),
    new Phases.AcquireInputsPhase(),
    new Phases.TransportInventoryPhase(),
    new Phases.RunProductionPhase(),
    new Phases.RestockRetailPhase(),
    new Phases.ResolveConsumerPurchasesPhase(),
    new Phases.SettleInvoicesAndWagesPhase(),
    new Phases.SettleFinancePhase(),
    new Phases.ApplyResearchProgressPhase(),
    new Phases.UpdateExpectationsPhase(),
    new Phases.CloseAccountingPeriodPhase(),
    new Phases.EmitObservationsPhase(),
  ];
}
