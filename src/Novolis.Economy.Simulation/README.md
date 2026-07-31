# Novolis.Economy.Simulation

Deterministic economic **tick runner**: `EconomyWorld`, ordered phases, command queue, events, and world fingerprint hash.

Holds ops `LegalEntity` collections; `OwnershipClaim` lives in **Accounting**. Economic authority is **`Novolis.Economy.Core`** (`EconomyWorld.CoreState`); period close calls `EconomyEngine.Advance`.

`EconomicRegion` + `AddRegion` / household `AddCohort` living clamp; region labor pools; production slots for mfg/assembly only.

Does **not** reference `Novolis.Simulation.*` (spatial stack).

## Install

```bash
dotnet add package Novolis.Economy.Simulation
```

## Quick start

```csharp
using Novolis.Economy;
using Novolis.Economy.Production;
using Novolis.Economy.Simulation;

using Novolis.Economy.Simulation.Extensions;

var world = new EconomyWorldBuilder()
  .AddRegion(areaId, livingCapacityHouseholds: 10_000, productionSlots: 50)
  .Build();
var sim = new EconomySimulation(seed: 42, world);

sim.Enqueue(new SetProductionPlan(firmId, facilityId, productId, Quantity.From(50m)));
await sim.AdvanceAsync(SimulationDuration.FromHours(24));

Console.WriteLine(WorldReportFormatter.Format(world.ToReportSnapshot()));
```

Custom phase order (tests): `new EconomySimulation(seed, world, PhasePipeline.CreateDefault())`.

## API

| Type | Role |
|------|------|
| `EconomySimulation` | Command queue, `AdvanceAsync`, throughput mode |
| `IEconomySimulation` | Simulation contract |
| `EconomyWorld` | Firms, regions, inventory, ledgers, loans, hub book |
| `EconomyWorldBuilder` | Fluent world setup (`AddRegion`, `AddFirm`, `AddProduct`, …) |
| `EconomyWorldExtensions` | `ToReportSnapshot()` |
| `PhasePipeline` / `DefaultPhases` | Ordered hourly + period-close phases |
| `ISimulationPhase` | Single phase hook |
| `SimulationPhaseOrder` | Phase enum ordering |
| `CoreEconomyBridge` | Sync ops holdings ↔ Core BM state |
| `DefaultConsequenceEngine` | Post-command side effects |
| `LegalEntity` / `LegalEntityKind` | Ops party records |
| `CohortBudgetResetMode` | When cohort budgets refresh |
| `MoneyStock` | Aggregate money diagnostics |
| `WorldReportFormatter` | `Format(WorldReportSnapshot)` text report |

## Dogfooding / apps

Composition root for [`novolis-dogfooding`](https://github.com/Novolis-Platform/novolis-dogfooding) `apps/economy/` (`EconomyBoard`, `TrampFreighterPlay`).

## Related

| Package | Role |
|---------|------|
| `Novolis.Economy.Core` | BM kernel advanced at period boundaries |
| `Novolis.Economy.Production` | Command / event types enqueued here |
| `Novolis.Economy.Agents` | Tick agents before each hour |
| `Novolis.Economy.Finance` | `SettleFinance` uses `LoanEngine` |
