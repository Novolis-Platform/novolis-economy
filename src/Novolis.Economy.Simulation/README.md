# Novolis.Economy.Simulation

Deterministic economic tick runner: `EconomyWorld`, ordered phases, command queue, events, and world fingerprint hash.

Holds ops `LegalEntity` collections; `OwnershipClaim` lives in **Accounting**. Economic authority is **`Novolis.Economy.Core`** (`EconomyWorld.CoreState`); period close calls `EconomyEngine.Advance`.

`EconomicRegion` + `AddRegion` / household `AddCohort` living clamp; region labor pools; production slots for mfg/assembly only.

```bash
dotnet add package Novolis.Economy.Simulation
```

```csharp
var sim = new EconomySimulation(seed: 42, world);
sim.Enqueue(new SetProductionPlan(...));
await sim.AdvanceAsync(SimulationDuration.FromHours(24));
```

Does **not** reference `Novolis.Simulation.*` (spatial stack).
