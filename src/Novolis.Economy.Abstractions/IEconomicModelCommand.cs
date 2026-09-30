using Novolis.Economy.Primitives;

namespace Novolis.Economy.Abstractions;

/// <summary>Action supplied by a game, actor host, or research harness.</summary>
public interface IEconomicModelCommand
{
    string Kind { get; }
}
