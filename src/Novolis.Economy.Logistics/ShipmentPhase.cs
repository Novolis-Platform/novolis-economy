using System.Collections.Immutable;
using Novolis.Economy;

namespace Novolis.Economy.Logistics;

/// <summary>Phase of a multi-leg shipment.</summary>
public enum ShipmentPhase
{
  /// <summary>Loading at origin hub.</summary>
  Loading = 0,
  /// <summary>Moving along a corridor.</summary>
  Underway = 1,
  /// <summary>Unloading / bunkering at an intermediate or final hub.</summary>
  Unloading = 2,
  /// <summary>Waiting for a berth.</summary>
  WaitingBerth = 3,
  /// <summary>Complete.</summary>
  Delivered = 4,
  /// <summary>Failed / cancelled.</summary>
  Cancelled = 5,
}
