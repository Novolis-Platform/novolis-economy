using Novolis.Economy.Core;

namespace Novolis.Economy.Simulation.Bounded;

/// <summary>
/// Advances the Core period marker for operational Simulation models.
/// Hourly phases already apply their domain effects; this step prevents the
/// bounded teaching economy from being run a second time at period close.
/// </summary>
public sealed class AdvancePeriodBoundaryStep : IBoundedPeriodStep
{
  /// <summary>Stable diagnostic name.</summary>
  public string Name => "period-boundary";

  /// <summary>Closes one operational period without duplicating hourly effects.</summary>
  public BoundedPeriodState Execute(BoundedPeriodState current) =>
    current with
    {
      Period = checked(current.Period + 1),
      Flows = PeriodFlowLedger.Empty,
      Scratch = BoundedPeriodScratch.Empty
    };
}
