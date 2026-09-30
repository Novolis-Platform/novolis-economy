using System.Collections.Immutable;
using Novolis.Economy;

namespace Novolis.Economy.Production;

/// <summary>Input line in a product recipe.</summary>
/// <param name="ProductId">Required input product.</param>
/// <param name="QuantityPerOutput">Input quantity consumed per output unit.</param>
public sealed record ProductInput(ProductId ProductId, Quantity QuantityPerOutput);
