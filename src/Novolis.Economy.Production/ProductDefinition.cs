using System.Collections.Immutable;
using Novolis.Economy;

namespace Novolis.Economy.Production;

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
