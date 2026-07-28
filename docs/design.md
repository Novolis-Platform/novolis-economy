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
Novolis.Economy                 primitives, IDs, markers, RNG
Novolis.Economy.Production      recipes, batches, facility layout stubs
Novolis.Economy.Markets         market estimates / intelligence stubs
Novolis.Economy.Accounting      ledger stubs
Novolis.Economy.Logistics       shipments and routes stubs
Novolis.Economy.Population      consumer cohorts stubs
Novolis.Economy.Simulation      phase pipeline and IEconomySimulation
```

Domain packages depend only on `Novolis.Economy`. `Novolis.Economy.Simulation` references all domain packages so the tick runner can host phases that will later call into them.

## Commands, events, projections

- **Commands** (`IEconomyCommand`) — decisions (player or AI)
- **Events** (`IEconomyEvent`) — what occurred (diagnostics, replay, reporting)
- **Projections** (`IEconomyProjection`) — read models for UI queries

Persistence is **periodic full snapshots**, not full event sourcing. Events are retained for explainability and short-horizon replay.

## Simulation phases

Ordered phases run every economic hour:

1. Apply decisions
2. Allocate labor
3. Acquire inputs
4. Transport inventory
5. Run production
6. Restock retail
7. Resolve consumer purchases
8. Settle invoices and wages
9. Apply research progress
10. Update expectations and market knowledge
11. Close accounting period (when due)
12. Emit observations

Skeleton phases are record-only (diagnostic events). Algorithms land in a later vertical slice.

## Determinism

Identical seed + identical command stream must produce identical `SimulationState.Hash` after the same number of ticks.
