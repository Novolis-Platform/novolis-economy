using Novolis.Economy.Abstractions;

namespace Novolis.Economy;

/// <summary>Marker for player or AI decisions applied to the simulation.</summary>
public interface IEconomyCommand : IEconomicModelCommand
{
  string IEconomicModelCommand.Kind => GetType().Name;
}
