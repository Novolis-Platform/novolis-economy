using System.Collections.Immutable;
using Novolis.Economy;

namespace Novolis.Economy.Production;

/// <summary>Physical inventory lot with cost and quality.</summary>
/// <param name="ProductId">Product type.</param>
/// <param name="Quantity">Lot size.</param>
/// <param name="Quality">Measured quality.</param>
/// <param name="UnitCost">Accounting unit cost.</param>
/// <param name="ProducedAt">Production date.</param>
/// <param name="BrandId">Optional brand.</param>
public sealed record ProductBatch(
  ProductId ProductId,
  Quantity Quantity,
  ProductQuality Quality,
  Money UnitCost,
  SimulationDate ProducedAt,
  BrandId? BrandId);
