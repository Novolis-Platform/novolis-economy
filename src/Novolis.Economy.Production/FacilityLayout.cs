using System.Collections.Immutable;
using Novolis.Economy;

namespace Novolis.Economy.Production;

/// <summary>Facility as a graph of operating units and routes.</summary>
/// <param name="Units">Operating units by id.</param>
/// <param name="Routes">Material routes.</param>
public sealed record FacilityLayout(
  ImmutableDictionary<OperatingUnitId, OperatingUnit> Units,
  ImmutableArray<MaterialRoute> Routes);
