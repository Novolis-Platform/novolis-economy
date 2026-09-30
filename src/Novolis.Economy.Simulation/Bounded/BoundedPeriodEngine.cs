using Novolis.Economy.Core;

namespace Novolis.Economy.Simulation.Bounded;

/// <summary>
/// Executes the bounded profile's ordered period transitions.
/// Core supplies atomic state transitions; Simulation owns this orchestration.
/// </summary>
public sealed class BoundedPeriodEngine
{
  /// <summary>Creates an ordered bounded period engine.</summary>
  public BoundedPeriodEngine(
    IReadOnlyList<IBoundedPeriodStep> steps,
    EconomyModelSpecification? specification = null)
  {
    Steps = steps ?? throw new ArgumentNullException(nameof(steps));
    Specification = specification ?? EconomyModelSpecification.Default;
  }

  /// <summary>Configured transitions in execution order.</summary>
  public IReadOnlyList<IBoundedPeriodStep> Steps { get; }

  /// <summary>Behavioral parameters supplied to the bounded profile.</summary>
  public EconomyModelSpecification Specification { get; }

  /// <summary>Applies all configured transitions once.</summary>
  public BoundedPeriodState Advance(EconomyState state) =>
    Advance(new BoundedPeriodState(state, specification: Specification));

  /// <summary>Applies all configured transitions to a wrapped run state.</summary>
  public BoundedPeriodState Advance(BoundedPeriodState state)
  {
    ArgumentNullException.ThrowIfNull(state);
    return Steps.Aggregate(
      state,
      static (next, step) => step.Execute(next));
  }
}
