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

  /// <summary>Match hub spot buy/sell orders at the same location.</summary>
  MatchHubOrders = 4,

  /// <summary>Transport inventory.</summary>
  TransportInventory = 5,

  /// <summary>Run production.</summary>
  RunProduction = 6,

  /// <summary>Restock retail.</summary>
  RestockRetail = 7,

  /// <summary>Resolve consumer purchases.</summary>
  ResolveConsumerPurchases = 8,

  /// <summary>Settle invoices and wages.</summary>
  SettleInvoicesAndWages = 9,

  /// <summary>Apply research progress.</summary>
  ApplyResearchProgress = 10,

  /// <summary>Update expectations and market knowledge.</summary>
  UpdateExpectations = 11,

  /// <summary>Close accounting period when due.</summary>
  CloseAccountingPeriod = 12,

  /// <summary>Emit observations for projections and diagnostics.</summary>
  EmitObservations = 13,
}
