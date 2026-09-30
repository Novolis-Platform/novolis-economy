using System.Collections.Immutable;
using Novolis.Economy;

namespace Novolis.Economy.Production;

/// <summary>Optional shelf-life in simulation hours.</summary>
/// <param name="Hours">Hours until spoilage.</param>
public readonly record struct ShelfLife(long Hours);
