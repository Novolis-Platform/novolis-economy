# Getting started

`novolis-economy` ships NuGet packages for headless, deterministic economic simulation.

## Install

```bash
dotnet add package Novolis.Economy.Core
dotnet add package Novolis.Economy.Simulation
```

Restore from GitHub Packages (`2026.1.*`) per [novolis-governance package policy](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/package-policy.md).

**Breaking:** PackageId `Novolis.Economy` is retired — use `Novolis.Economy.Core` for money, entities, regions, resources, and the period pipeline. Ops types (Quantity, hubs, commands) come from the ops packages Simulation already depends on.

## Quick start

```csharp
using Novolis.Economy;
using Novolis.Economy.Core;
using Novolis.Economy.Simulation;

var world = new EconomyWorldBuilder()
    .AddFirm(FirmId.From(guid), "Acme", Money.From(10_000m))
    // ... products, facilities, inventory, cohorts ...
    .Build();

var sim = new EconomySimulation(seed: 42, world);
sim.Enqueue(new SetRetailPrice(firm, facility, product, Money.From(5m)));
sim.Enqueue(new SetProductionPlan(firm, facility, product, Quantity.From(10m)));
await sim.AdvanceAsync(SimulationDuration.FromHours(24));
// Period boundaries run EconomyEngine.Advance on world.CoreState
```

## Build and test

```powershell
dotnet build Novolis.Economy.slnx
dotnet test --project tests/Novolis.Economy.Unit
dotnet pack Novolis.Economy.slnx -c Release -o artifacts/packages
```

Packages publish to **GitHub Packages** on merge to `main`.

## Next steps

- [Design](design.md) for world model, Core pivot, and phases
- [Concept](concept.md) for product framing
- [Release](release.md) for versioning
