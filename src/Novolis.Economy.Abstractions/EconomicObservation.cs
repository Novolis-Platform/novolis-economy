using Novolis.Economy.Primitives;

namespace Novolis.Economy.Abstractions;

/// <summary>Small, unit-labelled scalar observation emitted by a model tick.</summary>
public sealed record EconomicObservation(
    string Name,
    decimal Value,
    string Unit,
    string? Description = null);
