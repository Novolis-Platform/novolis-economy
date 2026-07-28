# Design

## Stack position

`Novolis.Economy.*` is a **separate domain family** from the Math → Physics → Simulation stack.

| Family | Owns |
|--------|------|
| `Novolis.Simulation.*` | Spatial/physics orchestration: worlds, cameras, kinematics |
| `Novolis.Economy.*` | Economic processes: products, markets, accounting, logistics, population, economic ticks |

This repo must **not** reference `Novolis.Simulation.*`, Raylib, or product hosts. Future `novolis-commerce` consumes Economy via NuGet and may compose snapshots from `novolis-workspaces` at the product layer.

## Package split

```text
Novolis.Economy                 primitives, IDs, markers, RNG, commands/events
Novolis.Economy.Production      recipes, batches, inventory store, production engine
Novolis.Economy.Markets         market estimates / observed trade book
Novolis.Economy.Accounting      ledger, invoices, double-entry posting
Novolis.Economy.Logistics       freight routes, active shipments, logistics engine
Novolis.Economy.Population      cohorts, preference-weighted demand engine
Novolis.Economy.Simulation      EconomyWorld, phase pipeline, IEconomySimulation
```

## World model

`SimulationState.World` (`EconomyWorld`) holds:

- Product catalog and facility layouts
- Firm ledgers (cash, inventory, revenue, COGS, wages, equity)
- FIFO inventory lots by `(firm, location, product)`
- Posted retail prices and production plans
- Freight routes / in-flight shipments
- Consumer cohorts with period budgets
- Observed market trade book

Seed worlds with `EconomyWorldBuilder` (deterministic ids via fixed guids).

## Commands, events, projections

- **Commands** — decisions (`SetRetailPrice`, `SetProductionPlan`, `PlaceProcurementOrder`, `IssueShipment`, `SetAvailableLabor`, …)
- **Events** — what occurred (`BatchProduced`, `GoodsSold`, `ShipmentDelivered`, …)
- **Projections** — read models (`ProductMarketView`)

Persistence intent remains **periodic full snapshots**, not full event sourcing.

## Simulation phases

Ordered phases run every economic hour and mutate the world:

1. Apply decisions
2. Allocate labor (+ wage accrual)
3. Acquire inputs (exogenous procurement + dispatch requests)
4. Transport inventory
5. Run production (+ optional spoilage)
6. Restock retail (auto-ship storage → retail)
7. Resolve consumer purchases (posted-price, stock-constrained)
8. Settle invoices and wages
9. Apply research progress (productivity coefficient)
10. Update expectations (market book ready)
11. Close accounting period (budget reset)
12. Emit observations

## Determinism

Identical seed + identical command stream + identical initial world must produce identical `SimulationState.Hash` after the same number of ticks. Hash covers clock, RNG, event count, and a world fingerprint (cash/inventory/prices/shipments/cohort budgets).

## Non-goals

UI/host, AI firm controllers, gamification (XP, morale meters), soft loans/bankruptcy drama, full general-equilibrium solvers.
