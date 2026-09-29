<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-economy">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Economy.Agents

**Rules-based economic actors** — policies that observe a read-only world view
and return or enqueue bounded command values. They may use deterministic
named randomness and bounded fuzzing; they are not LLMs or ML.

Simulation schedules agents **before** each simulation hour. Settlement
(production, finance, logistics) happens inside Simulation phases and Core
commits the resulting economic transitions.

## Install

```bash
dotnet add package Novolis.Economy.Agents
```

The assembly does not reference `Novolis.Economy.Simulation`. It depends on
the model-neutral `Novolis.Economy.Abstractions` boundary and the domain
packages whose commands and observations its actor policies use.

## Quick start

```csharp
using Novolis.Economy;
using Novolis.Economy.Agents;
using Novolis.Economy.Simulation;

var sim = new EconomySimulation(seed: 42, world);
var agents = new IEconomicAgent[]
{
  new ManufacturingFirmAgent(mfgFirmId, mfgPolicy),
  new RetailFirmAgent(retailFirmId, retailPolicy),
};

var rng = new DeterministicRandom(sim.State.Seed);
var ctx = new AgentContext(
  sim.State.World,
  sim.State.Clock,
  rng,
  sim.Enqueue);
AgentScheduler.TickAll(agents, ctx);
await sim.AdvanceAsync(SimulationDuration.FromHours(1));
```

Each agent exposes `LastDecision` for dashboards. `HubOrderQuotes.CancelOpen` clears stale hub orders before reposting quotes.

## API

| Type | Role |
|------|------|
| `IEconomicAgent` | Actor identity, status, and `Tick(AgentContext)` |
| `AgentContext` | Read-only world, clock, RNG, and command sink |
| `AgentScheduler` | `TickAll(agents, context)` |
| `AgentSite` | Inventory location + optional facility / hub binding |
| `HubOrderQuotes` | Cancel open hub orders for a firm |
| `ExtractiveFirmAgent` | Extract primary resource, sell on hub book |
| `ManufacturingFirmAgent` | Buy inputs, run throttled plans, sell outputs |
| `RetailFirmAgent` | Restock, set retail prices, sell to cohorts |
| `CarrierFirmAgent` | Plan multi-leg shipments on the hub network |
| `TreasuryFirmAgent` | Originate / repay inter-firm loans |
| `HouseholdFirmAgent` | Invest / lend only above cohort comfort (`BudgetRemaining`) |
| `*FirmAgentPolicy` records | Per-agent thresholds (sites, SKUs, loan terms, …) |

## Dogfooding / apps

Used by [`novolis-dogfooding`](https://github.com/Novolis-Platform/novolis-dogfooding) economy apps (`EconomyBoard`, `TrampFreighterPlay`) under `apps/economy/`.

## Related

| Package | Role |
|---------|------|
| `Novolis.Economy.Abstractions` | Rule, actor, and deterministic-random contracts |
| `Novolis.Economy.Simulation` | Schedules actors and applies their commands |
| `Novolis.Economy.Production` | Recipes, inventory, hub orders, loan commands |
| `Novolis.Economy.Markets` | Observed tape, pricing helpers |
| `Novolis.Economy.Finance` | Loan settlement engine |

