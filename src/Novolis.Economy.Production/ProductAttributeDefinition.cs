using System.Collections.Immutable;
using Novolis.Economy;

namespace Novolis.Economy.Production;

/// <summary>Attribute definition for a product type (skeleton).</summary>
/// <param name="Name">Attribute name.</param>
/// <param name="Unit">Display unit label.</param>
public sealed record ProductAttributeDefinition(string Name, string Unit);
