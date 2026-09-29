using Novolis.Economy.Agents;

namespace Novolis.Economy.Simulation;

/// <summary>Stable identity of a complete economic model composition.</summary>
public sealed record SimulationModelIdentity(
    string Id,
    string Version)
{
  /// <summary>Identity retained by legacy hosts until they select a named model.</summary>
  public static SimulationModelIdentity Legacy(EconomyModelSpecification specification) =>
    new("LegacyOperational", specification.Version);
}

/// <summary>
/// Composition surface for one complete economic model. Domain libraries
/// provide mechanisms; the model selects their rules, actors, parameters, and
/// phase order. Simulation owns execution.
/// </summary>
public abstract class SimulationModelDefinition
{
  /// <summary>Stable model identity.</summary>
  public abstract SimulationModelIdentity Identity { get; }

  /// <summary>Serializable behavioral assumptions.</summary>
  public abstract EconomyModelSpecification Specification { get; }

  /// <summary>Selected domain rule identities.</summary>
  public virtual IReadOnlyList<string> RuleIdentities => Array.Empty<string>();

  /// <summary>Selected actor profile identities.</summary>
  public virtual IReadOnlyList<string> AgentProfileIds => Array.Empty<string>();

  /// <summary>Additional serializable inputs selected by this model.</summary>
  public virtual object ReproducibilityDescriptor => new { };

  /// <summary>Creates the initial world for this model.</summary>
  public virtual EconomyWorld CreateWorld(ulong seed) =>
    new(modelSpecification: Specification);

  /// <summary>Creates the hourly phase pipeline for this model.</summary>
  public virtual PhasePipeline CreatePipeline(EconomyWorld world) =>
    PhasePipeline.CreateDefault();

  /// <summary>Creates the Simulation-owned period runner for this model.</summary>
  public virtual Bounded.BoundedPeriodEngine CreatePeriodEngine(EconomyWorld world) =>
    Bounded.DefaultBoundedPeriodPipeline.CreateEngine(Specification);

  /// <summary>Creates the actors selected by this model.</summary>
  public virtual IReadOnlyList<IEconomicAgent> CreateAgents(
    EconomyWorld world,
    ulong seed) =>
    Array.Empty<IEconomicAgent>();
}

/// <summary>Model identity and rule selection used by legacy world hosts.</summary>
public sealed class LegacySimulationModel : SimulationModelDefinition
{
  private readonly EconomyModelSpecification _specification;

  /// <summary>Creates the compatibility model around a specification.</summary>
  public LegacySimulationModel(EconomyModelSpecification? specification = null) =>
    _specification = specification ?? EconomyModelSpecification.Default;

  /// <inheritdoc />
  public override SimulationModelIdentity Identity =>
    SimulationModelIdentity.Legacy(_specification);

  /// <inheritdoc />
  public override EconomyModelSpecification Specification => _specification;

  /// <inheritdoc />
  public override IReadOnlyList<string> RuleIdentities =>
    ["legacy.hourly-phases", "legacy.period-close"];
}
