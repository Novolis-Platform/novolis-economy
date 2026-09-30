using System.Collections.Immutable;
using Novolis.Economy;

namespace Novolis.Economy.Logistics;

/// <summary>Legacy shipment status.</summary>
public enum ShipmentStatus
{
  /// <summary>Queued.</summary>
  Queued = 0,
  /// <summary>In transit (legacy single-leg or multi-leg underway).</summary>
  InTransit = 1,
  /// <summary>Delivered.</summary>
  Delivered = 2,
  /// <summary>Cancelled.</summary>
  Cancelled = 3,
}
