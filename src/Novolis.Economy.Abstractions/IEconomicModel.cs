using Novolis.Economy.Primitives;

namespace Novolis.Economy.Abstractions;

/// <summary>
/// Host-neutral economic model. A model describes rules and applies one tick;
/// it does not own a clock, run lifecycle, scheduler, or product host.
/// </summary>
public interface IEconomicModel
{
    EconomicModelIdentity Identity { get; }

    IEconomicModelSpecification Specification { get; }

    IReadOnlyList<RuleIdentity> Rules { get; }

    IReadOnlyList<string> ActorProfileIds { get; }

    IEconomicModelState CreateState(IEconomicScenario scenario);

    EconomicTickResult Advance(
        IEconomicModelState state,
        EconomicTickContext context);
}
