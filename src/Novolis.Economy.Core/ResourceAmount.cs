namespace Novolis.Economy.Core;

/// <summary>Quantity of a resource.</summary>
public sealed record ResourceAmount(ResourceId ResourceId, decimal Quantity);