namespace Novolis.Economy.Simulation;

/// <summary>Identity metadata for a reproducible simulation run.</summary>
public sealed record SimulationRunManifest(
  string ModelVersion,
  ulong RootSeed,
  ulong InitialStateFingerprint,
  int PeriodHours,
  string LibraryVersion);
