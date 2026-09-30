using System.Collections.Immutable;
using Novolis.Economy;

namespace Novolis.Economy.Production;

/// <summary>Directed material route between operating units.</summary>
/// <param name="From">Source unit.</param>
/// <param name="To">Destination unit.</param>
/// <param name="ProductId">Product moved (optional null = any).</param>
public sealed record MaterialRoute(
  OperatingUnitId From,
  OperatingUnitId To,
  ProductId? ProductId);
