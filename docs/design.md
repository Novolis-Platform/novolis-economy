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
Novolis.Economy.Logistics       hubs, corridors, vehicles, itineraries, multi-leg shipments
Novolis.Economy.Population      cohorts, preference-weighted demand engine
Novolis.Economy.Simulation      EconomyWorld, phase pipeline, IEconomySimulation
```

## World model

`SimulationState.World` (`EconomyWorld`) holds:

- Product catalog and facility layouts
- Firm ledgers (cash, inventory, revenue, COGS, wages, equity, transport fuel/toll expense)
- FIFO inventory lots by `(firm, location, product)`
- Posted retail prices and production plans
- Freight routes (compat shim) / hub–corridor graph / vehicle classes / in-flight shipments
- Consumer cohorts with period budgets
- Observed market trade book

Seed worlds with `EconomyWorldBuilder` (deterministic ids via fixed guids).

## Transport economics

Transport is modeled as an **economic** system (time, fuel stock, crew labor, capacity, topology)—not a map product and not coupled to `Novolis.Astro.*`.

| Economic concept | Terrestrial | Space (same abstractions) |
|------------------|-------------|---------------------------|
| Hub | Port, rail yard, transfer warehouse | Starport / jump staging dock |
| Corridor / leg | Highway segment, sea lane | Allowed hop between hubs |
| Range / bunker stop | Gas station / bunker port | Must refuel or stage at hub |
| Capacity | Truck/ship tonnage, berth limits | Hold / jump tonnage |
| Dwell | Loading hours at warehouse | Dock turnaround |
| In-transit inventory | Goods on the road | Goods in the hold (working capital locked) |

### Types (`Novolis.Economy.Logistics`)

- **`TransportHub`** — inventory location + dwell hours + optional berth capacity
- **`TransportCorridor`** — directed hub→hub, transit hours, max cargo, difficulty (fuel scales with hours×difficulty), toll money
- **`VehicleClass`** — cargo capacity, fuel burn per difficulty-hour, crew labor per underway hour, tank capacity
- **`Itinerary`** — ordered corridor ids; **`ItineraryPlanner`** finds a feasible path (Dijkstra on transit hours; legs exceeding tank capacity are unreachable)
- **`ActiveShipment`** — multi-leg phases (`Loading` / `Underway` / `Unloading` / `WaitingBerth`) or legacy single-leg via **`FreightRoute`**

Default fuel model: fuel is an **inventory commodity** at hubs; bunkering fills the tank for the next leg when onboard fuel is short. Tolls debit cash (`TransportTollExpense`). Burn writes off inventory to `TransportFuelExpense`. Crew hours while `Underway` are included in labor allocation / wage accrual. Cargo remains off destination shelves until delivery (**working capital lockup**).

Command: `PlanShipment(Firm, OriginHub, DestHub, Product, Qty, VehicleClass)`. Events: `ShipmentLegStarted`, `ShipmentHubArrived`, `FuelBunkered`, `TransportTollPaid`, `ShipmentDelivered`, `ShipmentPlanFailed`.

### Non-goals (transport)

- `Novolis.Astro.*` package references or star catalogs inside Economy
- Graphics / StarMap / tycoon gameplay loop
- Full vehicle fleets with maintenance minigames
- Continuous spaceflight physics (`Physics.Orbits`)

## Commands, events, projections

- **Commands** — decisions (`SetRetailPrice`, `SetProductionPlan`, `PlaceProcurementOrder`, `IssueShipment`, `PlanShipment`, `SetAvailableLabor`, …)
- **Events** — what occurred (`BatchProduced`, `GoodsSold`, `ShipmentDelivered`, `ShipmentLegStarted`, …)
- **Projections** — read models (`ProductMarketView`)

Persistence intent remains **periodic full snapshots**, not full event sourcing.

## Simulation phases

Ordered phases run every economic hour and mutate the world:

1. Apply decisions
2. Allocate labor (+ manufacturing + underway crew wage accrual)
3. Acquire inputs (exogenous procurement + `IssueShipment` / `PlanShipment` dispatch)
4. Transport inventory (multi-leg advance, bunkering, tolls, fuel burn)
5. Run production (+ optional spoilage)
6. Restock retail (auto-ship storage → retail via FreightRoute shim)
7. Resolve consumer purchases (posted-price, stock-constrained)
8. Settle invoices and wages
9. Apply research progress (productivity coefficient)
10. Update expectations (market book ready)
11. Close accounting period (budget reset)
12. Emit observations

## Determinism

Identical seed + identical command stream + identical initial world must produce identical `SimulationState.Hash` after the same number of ticks. Hash covers clock, RNG, event count, and a world fingerprint (cash/inventory/prices/shipments/cohort budgets).

## Accounting / population — money conservation modes

Defaults preserve the open mint used by tramp / commodity-chain scenarios. Closed-loop polity scenarios opt in via `EconomyPolicy`:

| Knob | Default | Closed-loop |
|------|---------|-------------|
| `HouseholdCreditFromWages` | `false` (wage cash leaves the system) | `true` — paid wages raise cohort `BudgetRemaining` (population-weighted); emits `HouseholdCreditsIssued` |
| `CohortBudgetResetMode` | `MintFromDisposableIncome` | `CarryForward` — period close does not remint budgets |
| `TollBeneficiaryFirmId` | `null` (toll expense burns cash) | set to a treasury firm — shipper debit + beneficiary cash/revenue |

Inter-firm spot sales use `TransferGoodsForCash` (FIFO stock move + `PostCashSale` / `PostCashPurchase`); success emits `GoodsSoldInterFirm`, failure emits `TransferGoodsFailed` (`cash` / `stock`).

## Pricing / demand primitives

- **Area-local retail:** `FacilityBinding.Area` optional; `DemandEngine` only clears offers whose facility area matches the cohort (null facility area = global, for legacy scenarios).
- **`HaulCostEstimator`** (Logistics) — pure fuel + toll + crew cost for an itinerary.
- **`InventoryPressurePricing`** (Markets) — soft posted-price premium/discount from on-hand vs target stock.

## Non-goals

UI/host, AI firm controllers, gamification (XP, morale meters), soft loans/bankruptcy drama, full general-equilibrium solvers, Astro coupling, tycoon UI, continuous orbital physics.
