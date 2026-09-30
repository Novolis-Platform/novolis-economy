using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Cancel an open hub order.</summary>
public sealed record CancelHubOrder(Guid OrderId) : IEconomyCommand;
