using Novolis.Economy.Primitives;

namespace Novolis.Economy.Abstractions;

/// <summary>Model-neutral contract for a pure rule over a context.</summary>
public interface IEconomicRule<in TContext, out TResult>
{
    RuleIdentity Identity { get; }

    TResult Apply(TContext context);
}
