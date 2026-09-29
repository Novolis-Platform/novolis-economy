<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-economy">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Economy.Simulation

Composition and execution layer for economic models: `EconomyWorld`, clocks,
ordered phases, model profiles, agents, run manifests, metrics, and stable
fingerprints.

Holds operational collections for orchestration; `OwnershipClaim` and
`FirmLedger` remain Accounting compatibility/input projections. Economic
authority is **`Novolis.Economy.Core`** (`EconomyWorld.CoreState`),
including positions, cash positions, claims, and Core share holdings. This
layer selects and runs the period engine; Core never owns the clock or phase
ordering.

`EconomicRegion` + `AddRegion` / household `AddCohort` living clamp; region labor pools; production slots for mfg/assembly only.

Registration is strict by default. Hosts that deliberately want game-friendly
implicit Core entities, assets, or regions must pass
`RegistrationMode.Implicit` to `EconomyWorld` or `EconomyWorldBuilder`.

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
var sim = EconomySimulation.FromModel(
  seed: 42,
  new Models.SmallOpenRegionalTradeModel());

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
| `SimulationModelDefinition` | Complete model composition surface |
| `SmallOpenRegionalTradeModel` | Documented flagship open regional model |
| `DeterministicBoundedModel` | Finite, replayable secondary profile |
| `SimulationRun` / `SimulationRunRequest` | Model-owned run execution and results |
| `SimulationRunManifest` | Reproducibility identity and selected rules |
| `SimulationMetrics` / `SimulationSweep` | Run observations and parameter sweeps |
| `EconomyWorld` | Firms, regions, inventory, ledgers, loans, hub book |
| `EconomyWorldBuilder` | Fluent world setup (`AddRegion`, `AddFirm`, `AddProduct`, …) |
| `EconomyWorldExtensions` | `ToReportSnapshot()` |
| `PhasePipeline` / `DefaultPhases` | Ordered hourly + period-close phases |
| `ISimulationPhase` | Single phase hook |
| `SimulationPhaseOrder` | Phase enum ordering |
| `CoreEconomyBridge` | Bind hub regions; period execution stays in the selected model |
| `CoreInventoryBridge` / `CoreCashBridge` | Reconcile operational detail into Core authority |
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

