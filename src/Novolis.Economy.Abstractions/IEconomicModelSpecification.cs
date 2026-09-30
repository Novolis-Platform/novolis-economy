using Novolis.Economy.Primitives;

namespace Novolis.Economy.Abstractions;

/// <summary>Serializable structural and institutional assumptions of a model.</summary>
public interface IEconomicModelSpecification
{
    string Version { get; }
}
