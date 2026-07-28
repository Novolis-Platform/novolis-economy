using System.Collections.Immutable;
using Novolis.Economy;

namespace Novolis.Economy.Production;

/// <summary>Input line in a product recipe.</summary>
/// <param name="ProductId">Required input product.</param>
/// <param name="QuantityPerOutput">Input quantity consumed per output unit.</param>
public sealed record ProductInput(ProductId ProductId, Quantity QuantityPerOutput);

/// <summary>Attribute definition for a product type (skeleton).</summary>
/// <param name="Name">Attribute name.</param>
/// <param name="Unit">Display unit label.</param>
public sealed record ProductAttributeDefinition(string Name, string Unit);

/// <summary>Optional shelf-life in simulation hours.</summary>
/// <param name="Hours">Hours until spoilage.</param>
public readonly record struct ShelfLife(long Hours);

/// <summary>Immutable product recipe definition.</summary>
/// <param name="Id">Product id.</param>
/// <param name="Category">Category id.</param>
/// <param name="Inputs">Recipe inputs.</param>
/// <param name="Attributes">Measurable attribute definitions.</param>
/// <param name="ProductionProcess">Process used to manufacture.</param>
/// <param name="ShelfLife">Optional spoilage horizon.</param>
public sealed record ProductDefinition(
  ProductId Id,
  ProductCategoryId Category,
  ImmutableArray<ProductInput> Inputs,
  ImmutableArray<ProductAttributeDefinition> Attributes,
  ProductionProcessId ProductionProcess,
  ShelfLife? ShelfLife);

/// <summary>Emergent quality score (0–100 skeleton scale).</summary>
/// <param name="Score">Quality points.</param>
public readonly record struct ProductQuality(decimal Score);

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

/// <summary>Kind of operating unit in a facility.</summary>
public enum OperatingUnitKind
{
  /// <summary>Purchasing desk.</summary>
  Purchasing = 0,
  /// <summary>Storage.</summary>
  Storage = 1,
  /// <summary>Manufacturing.</summary>
  Manufacturing = 2,
  /// <summary>Assembly.</summary>
  Assembly = 3,
  /// <summary>Quality assurance.</summary>
  QualityAssurance = 4,
  /// <summary>Sales / retail.</summary>
  Sales = 5,
  /// <summary>Advertising.</summary>
  Advertising = 6,
  /// <summary>Research.</summary>
  Research = 7,
  /// <summary>Training.</summary>
  Training = 8,
  /// <summary>Dispatch / shipping.</summary>
  Dispatch = 9,
}

/// <summary>One node in a facility workflow graph.</summary>
/// <param name="Id">Unit id.</param>
/// <param name="Kind">Unit kind.</param>
/// <param name="Capacity">Throughput capacity stub.</param>
public sealed record OperatingUnit(
  OperatingUnitId Id,
  OperatingUnitKind Kind,
  Quantity Capacity);

/// <summary>Directed material route between operating units.</summary>
/// <param name="From">Source unit.</param>
/// <param name="To">Destination unit.</param>
/// <param name="ProductId">Product moved (optional null = any).</param>
public sealed record MaterialRoute(
  OperatingUnitId From,
  OperatingUnitId To,
  ProductId? ProductId);

/// <summary>Facility as a graph of operating units and routes.</summary>
/// <param name="Units">Operating units by id.</param>
/// <param name="Routes">Material routes.</param>
public sealed record FacilityLayout(
  ImmutableDictionary<OperatingUnitId, OperatingUnit> Units,
  ImmutableArray<MaterialRoute> Routes);
