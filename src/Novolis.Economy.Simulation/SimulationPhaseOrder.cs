namespace Novolis.Economy.Simulation;

/// <summary>Ordered economic simulation phases (one pass per hour).</summary>
public enum SimulationPhaseOrder
{
  /// <summary>Apply player and AI decisions.</summary>
  ApplyDecisions = 1,

  /// <summary>Allocate labor.</summary>
  AllocateLabor = 2,

  /// <summary>Acquire production inputs.</summary>
  AcquireInputs = 3,

  /// <summary>Transport inventory.</summary>
  TransportInventory = 4,

  /// <summary>Run production.</summary>
  RunProduction = 5,

  /// <summary>Restock retail.</summary>
  RestockRetail = 6,

  /// <summary>Resolve consumer purchases.</summary>
  ResolveConsumerPurchases = 7,

  /// <summary>Settle invoices and wages.</summary>
  SettleInvoicesAndWages = 8,

  /// <summary>Apply research progress.</summary>
  ApplyResearchProgress = 9,

  /// <summary>Update expectations and market knowledge.</summary>
  UpdateExpectations = 10,

  /// <summary>Close accounting period when due.</summary>
  CloseAccountingPeriod = 11,

  /// <summary>Emit observations for projections and diagnostics.</summary>
  EmitObservations = 12,
}
