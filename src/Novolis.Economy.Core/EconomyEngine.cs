namespace Novolis.Economy.Core;

/// <summary>One ordered transformation of the aggregate economy state.</summary>
public interface IEconomyStep
{
    /// <summary>Stable step name for diagnostics and ordering docs.</summary>
    string Name { get; }

    /// <summary>Returns the next state (prefer immutable updates).</summary>
    EconomyState Execute(EconomyState current);
}

/// <summary>Advances <see cref="EconomyState"/> by folding an ordered step list.</summary>
/// <param name="Steps">Steps in economic execution order.</param>
public sealed class EconomyEngine(IReadOnlyList<IEconomyStep> Steps)
{
    /// <summary>Configured steps.</summary>
    public IReadOnlyList<IEconomyStep> Steps { get; } = Steps ?? throw new ArgumentNullException(nameof(Steps));

    /// <summary>Applies every step once, in order.</summary>
    public EconomyState Advance(EconomyState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return Steps.Aggregate(state, static (current, step) => step.Execute(current));
    }
}
