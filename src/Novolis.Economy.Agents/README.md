# Novolis.Economy.Agents

**Heuristic economic agents** — firms that observe `EconomyWorld`, apply deterministic policies, and enqueue `IEconomyCommand` values. Not LLMs or ML.

Agents run **before** each simulation hour: `AgentScheduler.TickAll` then `await sim.AdvanceAsync(...)`. Settlement (production, finance, logistics) happens inside Simulation phases.

## Install

```bash
dotnet add package Novolis.Economy.Agents
```

Depends on `Novolis.Economy.Simulation`, `Novolis.Economy.Production`, and `Novolis.Economy.Markets`.

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
var ctx = new AgentContext(sim, rng);
AgentScheduler.TickAll(agents, ctx);
await sim.AdvanceAsync(SimulationDuration.FromHours(1));
```

Each agent exposes `LastDecision` for dashboards. `HubOrderQuotes.CancelOpen` clears stale hub orders before reposting quotes.

## API

| Type | Role |
|------|------|
| `IEconomicAgent` | `FirmId`, `LastDecision`, `Tick(AgentContext)` |
| `AgentContext` | Simulation handle, world, clock, RNG, `Enqueue` |
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
| `Novolis.Economy.Simulation` | Tick runner, command queue, world state |
| `Novolis.Economy.Production` | Recipes, inventory, hub orders, loan commands |
| `Novolis.Economy.Markets` | Observed tape, pricing helpers |
| `Novolis.Economy.Finance` | Loan settlement engine |
