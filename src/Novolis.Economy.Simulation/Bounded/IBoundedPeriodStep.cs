using Novolis.Economy.Core;

namespace Novolis.Economy.Simulation.Bounded;

/// <summary>One ordered transition in the deterministic bounded profile.</summary>
public interface IBoundedPeriodStep
{
  /// <summary>Stable step name for diagnostics and sequencing tests.</summary>
  string Name { get; }

  /// <summary>Applies one bounded-profile transition.</summary>
  BoundedPeriodState Execute(BoundedPeriodState current);
}
