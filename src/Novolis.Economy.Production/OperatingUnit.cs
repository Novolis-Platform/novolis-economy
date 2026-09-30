using System.Collections.Immutable;
using Novolis.Economy;

namespace Novolis.Economy.Production;

/// <summary>One node in a facility workflow graph.</summary>
/// <param name="Id">Unit id.</param>
/// <param name="Kind">Unit kind.</param>
/// <param name="Capacity">Throughput capacity stub.</param>
public sealed record OperatingUnit(
  OperatingUnitId Id,
  OperatingUnitKind Kind,
  Quantity Capacity);
