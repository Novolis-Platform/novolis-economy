# Novolis.Economy.Simulation

Deterministic economic tick runner: `EconomyWorld`, ordered phases, command queue, events, and world fingerprint hash.

```bash
dotnet add package Novolis.Economy.Simulation
```

```csharp
var sim = new EconomySimulation(seed: 42, world);
sim.Enqueue(new SetProductionPlan(...));
await sim.AdvanceAsync(SimulationDuration.FromHours(24));
```

Does **not** reference `Novolis.Simulation.*` (spatial stack).
