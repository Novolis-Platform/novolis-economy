# Design

## Stack position

`Novolis.Economy.*` is a **separate domain family** from the Math → Physics → Simulation stack.

| Family | Owns |
|--------|------|
| `Novolis.Simulation.*` | Spatial/physics orchestration: worlds, cameras, kinematics |
| `Novolis.Economy.*` | Economic processes: products, markets, accounting, logistics, population, economic ticks |

This repo must **not** reference `Novolis.Simulation.*`, Raylib, or product hosts. Future `novolis-commerce` consumes Economy via NuGet and may compose snapshots from `novolis-workspaces` at the product layer.

## Package split (economic grammar)

**PackageId `Novolis.Economy` is retired.** Economic authority is [`Novolis.Economy.Core`](../src/Novolis.Economy.Core/). Ops packages depend on Core; ops-only types live in the packages that use them (not Core).

```text
Novolis.Economy.Primitives     universal economic identities and scalar values
Novolis.Economy.Abstractions   model-neutral rule, capability, and actor contracts
Novolis.Economy.Core            authoritative positions, claims, transactions, and invariants
Novolis.Economy.Production      production mechanisms and product detail
Novolis.Economy.Markets         pricing, demand, and market mechanisms
Novolis.Economy.Finance         credit terms, interest, and default mechanisms
Novolis.Economy.Population      cohorts, labor, migration, and demand mechanisms
Novolis.Economy.Logistics       hubs, corridors, transport, and in-transit stock
Novolis.Economy.Accounting      read-only financial projections and diagnostics
Novolis.Economy.Agents          rules-based, optionally fuzzable actor policies
Novolis.Economy.Simulation      model composition, clocks, phases, runs, metrics, and manifests
```

`SmallOpenRegionalTrade` is the documented flagship Simulation model.
`DeterministicBounded` is the finite secondary profile for regression and
teaching. There is no `Novolis.Economy.Experiments` package; sweep and
repeatable-run support belongs to Simulation.

### Type migration map

| Former prim (`Novolis.Economy`) | Home after pivot |
|---------------------------------|------------------|
| `Money` | **Core** `Money` (global alias) |
| `FirmId` / party key | **Core** `LegalEntityId` (global alias `FirmId`) |
| `ProductId` | **Core** `ResourceId` (global alias `ProductId`) |
| `GeographicAreaId` | **Core** `RegionId` (global alias `GeographicAreaId`) |
| `ConsumerCohortId` | **Core** `CohortId` (global alias `ConsumerCohortId`) |
| `LoanId` | **Core** `LoanId` |
| `Quantity`, `Percentage` | **Production** (namespace `Novolis.Economy`) |
| `SimulationHour` / `Date` / `Duration` | **Logistics** (namespace `Novolis.Economy`; shared ops clock) |
| Facility / process / inventory / brand IDs | **Production** |
| Transport / shipment / route / vehicle IDs | **Logistics** |
| `IEconomyCommand` / events / RNG / markers | **Simulation** |
| Ops `LegalEntity` / `LegalEntityKind` (Firm/Civic/Household) | **Simulation** (distinct from Core BM kinds) |
| `OwnershipClaim` | **Accounting** |
| `HouseholdProductivity*` | **Population** |

### Time model

- **Hours** advance carriage (Logistics) and operational phases.
- **Simulation** selects the model-specific period runner and executes it at
  `PeriodHours` boundaries.
- **Core** validates and applies the atomic transitions produced by that
  runner; it does not own a scheduler, clock, or phase order.
- Do not enlarge Core with unused primitives.

**Frozen:** no new features on the deleted PackageId `Novolis.Economy`.

## World model

`SimulationState.World` (`EconomyWorld`) holds:

- Product catalog and facility layouts
- Operational compatibility ledgers and detailed inventory projections
- Legal-entity metadata and ownership claims (ops types in Simulation / Accounting; BM shares in Core)
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

## Model composition and Simulation phases

`SimulationModelDefinition` is the composition root. A model declares its
stable identity, specification, selected domain rules, actor profiles,
initial-world builder, hourly phases, and bounded period runner. Simulation
schedules agents and phases; agents return commands and Core commits the
resulting economic transitions.

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
- **Hub order book** — `PostHubOrder` / `MatchHubOrders` phase; local buy/sell at a location; carriers haul cross-hub.
- **`ProductionThrottle`** — taper rate as inventory approaches target.
- **`PriceElasticity`** policy → `DemandEngine` scales buy qty by relative price.
- **`MoneyStock.Liquid`** — firm cash + household budgets.
- **Finance** — `OriginateLoan` / `RepayLoan`, hourly interest onto notes, term default (`SettleFinance`) with **credit freeze**, facility absorb to lender, and ownership claim transfer.
- **Legal entity** — ops `LegalEntity` / `LegalEntityKind` (Firm/Civic/Household) live in **Simulation**; Core has its own BM `LegalEntityKind`. Simulation stores ops entities on the world and `CoreState` for the kernel.
- **Capacity** — `UpgradeFacility` spends cash and scales manufacturing/assembly unit capacity.
- **Agents** — heuristic or bounded-fuzz economic agents (`IEconomicAgent`)
  that observe a read-only view and enqueue commands; not ML. Treasury skips
  credit-frozen borrowers.
- **Accounting** — read-only projections can explain the financials of an
  entity, cohort, region, institutional group, or arbitrary group from Core
  state and transaction history. Finance does not depend on Accounting.
- **Households / regions** — `LegalEntityKind.Household` per cohort; spendable liquid is **only** `BudgetRemaining` (ledger cash unused for spending). `PopulationCount` is household count (no headcount). `HouseholdProductivityKind` Common/Mean/Extreme → 12/18/24 hours per household-day; region labor pool = `Households × HoursPerDay / 24` per tick. `EconomicRegion` living + production caps (mfg/assembly slots only). Comfort: invest/lend iff `BudgetRemaining > ComfortThresholdPerHousehold × Households` (default 50). Guards in ApplyDecisions. Wages credit cohorts in the facility's area. `PurchaseOwnership` / household `OriginateLoan` debit budget.
- **HouseholdFirmAgent** — comfort hold vs small lend/invest.

## Non-goals

UI/host, ML / LLM agents, gamification (XP, morale meters), full bankruptcy liquidation UI, binding labor scarcity tuning, full general-equilibrium solvers, Astro coupling, tycoon UI, continuous orbital physics, `Novolis.Economy.Civics` package (civic = firm kind + toll beneficiary), per-person entities, intra-habitat logistics.
