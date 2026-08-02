# Release

Packages publish as `Novolis.Economy.*` on GitHub Packages under the `2026.1.*` line.

## 2026.1.x — Exogenous export

- `PlaceExportOrder` / `ExportFilled` — sell inventory into infinite exogenous demand (mirror of procurement)
- NearSol: Sol Capital Raw export hub + comfortable-HH tramp ventures (hull loan entry)

## 2026.1.x — Core pivot (breaking)

- **PackageId `Novolis.Economy` retired** (no shim). Consumers must `PackageReference` **`Novolis.Economy.Core`**.
- Ops packages depend on Core; former prim types live in Production / Logistics / Accounting / Simulation / Population (see [design.md](design.md) type map).
- Time: hours advance carriage only; `EconomyEngine.Advance` runs at period boundaries via Simulation.
- Dogfood (`NearSol`, Tramp*, EconomyBoard) and Sins (`Novolis.Economy.Core` only) consume GPR `2026.1.*`.

## 2026.1.x — Households, regions, labor, comfort

- `LegalEntityKind.Household`; `HouseholdProductivityKind` (12/18/24 hh-hours/day); `PurchaseOwnership`
- `PopulationCount` = household count (no headcount / people-per-household)
- `EconomicRegion` living/production caps; region labor pools; wages by facility area → `BudgetRemaining`
- Comfort guards on household lend/invest; single spendable wallet (`BudgetRemaining`)
- `HouseholdFirmAgent`; builder `AddRegion` + living clamp / mfg production slots

## 2026.1.x — Legal entity, ownership, capacity, default absorb

- `LegalEntity` (Firm / Civic) on `FirmId`, `RegistryId`, `CreditFrozen`
- Ownership claims + dividends; `UpgradeFacility` capacity investment
- On `LoanDefaulted`: freeze borrower credit, rebind facilities to lender, transfer issuer ownership claims
- Builder: `AddCivic`, `SetOwnership`

## 2026.1.x — Economic agents + finance

- `Novolis.Economy.Finance` — term loans, interest accrual, repayment, default hooks; `SettleFinance` phase
- `Novolis.Economy.Agents` — heuristic economic agents (Extractive / Manufacturing / Retail / Carrier / Treasury); not ML
- Account roles: NotesReceivable / NotesPayable / InterestIncome / InterestExpense

## 2026.1.x — Simulation perf

- Cache world fingerprint across `Enqueue` (recompute once per tick)
- Record `MarketTradeObserved` into `MarketBook` inline (no full-event scan each hour)
- Omit high-churn `HubOrderPosted` / `HubOrderCancelled` from the event log (fills still emit)

## 2026.1.x — Tycoon market primitives

- Hub spot order book (`PostHubOrder`, `MatchHubOrders` phase, `HubOrderFilled`)
- `ProductionThrottle`, `EconomyPolicy.PriceElasticity`, `MoneyStock.Liquid`

## 2026.1.x — Pricing / demand rigor

- Optional `FacilityBinding.Area` + area-filtered `DemandEngine` clearing
- `HaulCostEstimator` for itinerary variable cost
- `InventoryPressurePricing` for soft stock-driven posted prices

## 2026.1.x — Closed-loop credits + inter-firm transfer

- Policy: `HouseholdCreditFromWages`, `CohortBudgetResetMode` (`MintFromDisposableIncome` | `CarryForward`), `TollBeneficiaryFirmId`
- Wage settlement may credit household cohorts (`HouseholdCreditsIssued`)
- `TransferGoodsForCash` / `GoodsSoldInterFirm` / `TransferGoodsFailed` for firm↔firm inventory+cash
- Optional toll treasury credit (payer expense ↔ beneficiary revenue)

## 2026.1.x — Economic transport

- Hub / corridor / vehicle-class transport network (geography-agnostic)
- `ItineraryPlanner` + multi-leg `LogisticsEngine` (dwell, bunker fuel, berth queue, crew, tolls)
- `PlanShipment` command; ledger roles `TransportFuelExpense` / `TransportTollExpense`
- Hub-network scenario + machine-speed smoke aggregates; `FreightRoute` remains a single-leg shim

## 2026.1.0 — Economic kernel

- `EconomyWorld` + `EconomyWorldBuilder`
- Working phase pipeline: production, logistics, demand, labor, ledger settlement
- Double-entry `FirmLedger` / `LedgerEngine`
- Commodity-chain scenario tests (determinism + ledger balance)

## Policy

- [Release policy](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/release-policy.md)
- [Package policy](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/package-policy.md)

## Local validation

```powershell
dotnet build Novolis.Economy.slnx -c Release
dotnet test --project tests/Novolis.Economy.Unit -c Release
dotnet pack Novolis.Economy.slnx -c Release -o artifacts/packages
```

## Versioning

Bump via `build/version.json` / `build/version.props` per governance; consumers pin `2026.1.*` from GPR.
