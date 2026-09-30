namespace Novolis.Economy.Core;

/// <summary>Transform recipe for one activity run (SPEC §6).</summary>
public sealed record ActivityRecipe(
  IReadOnlyList<ResourceAmount> Inputs,
  IReadOnlyList<ResourceAmount> Outputs,
  decimal LaborHoursPerRun,
  decimal ProductionSpacePerRun);