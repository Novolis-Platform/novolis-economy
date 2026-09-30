using System.Collections.Immutable;
using Novolis.Economy;

namespace Novolis.Economy.Logistics;

/// <summary>Ordered path through corridors.</summary>
/// <param name="CorridorIds">Corridor sequence.</param>
public sealed record Itinerary(ImmutableArray<TransportCorridorId> CorridorIds)
{
  /// <summary>Empty itinerary.</summary>
  public static Itinerary Empty { get; } = new(ImmutableArray<TransportCorridorId>.Empty);

  /// <summary>Number of legs.</summary>
  public int LegCount => CorridorIds.Length;
}
