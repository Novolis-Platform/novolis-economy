# Getting started

`novolis-economy` ships NuGet packages for headless, deterministic economic simulation.

## Install

```bash
dotnet add package Novolis.Economy.Core
dotnet add package Novolis.Economy.Simulation
dotnet add package Novolis.Economy.Abstractions
```

Restore from GitHub Packages (`2026.1.*`) per [novolis-governance package policy](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/package-policy.md).

**Breaking:** PackageId `Novolis.Economy` is retired. Use `Novolis.Economy.Primitives`
for universal values, `Novolis.Economy.Core` for authoritative state
transitions, and `Novolis.Economy.Simulation` for model execution.

## Quick start

```csharp
using Novolis.Economy;
using Novolis.Economy.Core;
using Novolis.Economy.Simulation;

var world = new EconomyWorldBuilder()
    .AddFirm(FirmId.From(guid), "Acme", Money.From(10_000m))
    // ... products, facilities, inventory, cohorts ...
    .Build();

var sim = EconomySimulation.FromModel(
    seed: 42,
    new Novolis.Economy.Simulation.Models.SmallOpenRegionalTradeModel());
sim.Enqueue(new SetRetailPrice(firm, facility, product, Money.From(5m)));
sim.Enqueue(new SetProductionPlan(firm, facility, product, Quantity.From(10m)));
await sim.AdvanceAsync(SimulationDuration.FromHours(24));
// Simulation selects the model's period runner; Core remains the authority.
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
