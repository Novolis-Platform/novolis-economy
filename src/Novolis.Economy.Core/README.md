<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-economy">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Economy.Core

Bounded-minimum (BM) economic model: immutable `EconomyState`, ordered `IEconomyStep` fold, stock–flow discipline.
PackageId: `Novolis.Economy.Core` (`2026.1.*` on GitHub Packages). Normative types and rules: [`SPEC.md`](SPEC.md).

This package is the **economic kernel**. Ops packages (`Production`, `Logistics`, `Simulation`, …) depend on it. Hour ticks advance carriage only; Core’s 16-step pipeline settles at period boundaries via `EconomyWorld.CoreState` / `CoreEconomyBridge`.

---

## 1. Economic boundary

Core models **who owns what, where, and who owes whom**, plus **period flows** that update those stocks.

| In boundary | Out of boundary (see § Non-goals) |
|---|---|
| Legal entities (Household, Firm, Lender, Bank, Insurer, State) | Order books, continuous double auctions |
| Regions with living / production / logistics capacity | Individual workers, vehicles, pathfinding |
| Household cohorts + labor capacity | Participation-rate microfoundations |
| Activities (recipes, installed capacity) | R&D / tech trees |
| Resource holdings & transfers | Visual/map presentation |
| Shares, loans, credit facilities, deposits, insurance | Full Basel-style bank regulation |
| Payment obligations & settlement priority | Arrow–Debreu complete markets |
| State policy (tax / transfer) | Diplomacy, combat |

**Relationships (grammar):**

```text
HouseholdCohort ──lives in──► Region
Firm ──operates──► Activity ──in──► Region
Activity ──consumes/produces──► ResourceHolding (Owner × Region × Resource)
LegalEntity ──owes──► PaymentObligation ──to──► LegalEntity
Bank ──liability──► Deposit ──asset of──► Depositor
Loan ──asset of Lender / liability of Borrower──► (claim symmetry)
ShareHolding ──units of──► ShareClass(Issuer)
ResourceTransfer ──carriage──► (ownership preserved unless a sale)
```

**Ops weave:** Core `RegionId` ≈ hub / `GeographicAreaId`; deliveries credit Core holdings via `CoreEconomyBridge`. PackageId `Novolis.Economy` (primitives) is retired.

---

## 2. Legal entities

| Kind | Ownable | Issues shares | Notes |
|---|---|---|---|
| Household | No | No | Unownable beneficiary; labor & consumption |
| Firm | Yes | Yes | Operates activities |
| Lender | Yes | Yes | Lends owned/borrowed funds (cash transfer) |
| Bank | Yes | Yes | Accepts deposits; loan creates deposit |
| Insurer | Yes | Yes | Premiums / claims |
| State | No* | No | Fiscal & policy authority |

\*State is not a share issuer in BM. Validation: `EntityRules`.

---

## 3. Regions

Three capacities (SPEC §3):

- **Living** — household count ceiling (`RegionCapacity.OccupiedLiving` / `RemainingLiving`)
- **Production** — space for installed activity runs (`InstalledProductionSpace`)
- **Logistics** — quantity that may leave via transfers this period

Enforced in install/migrate/transfer paths and `InvariantChecker`.

---

## 4. Household cohorts

Cohorts are **aggregates**: `HouseholdCount × CashPerHousehold = TotalCash` (`HouseholdMath`).

Profiles: consumption weight, savings preference, labor quality, migration preference.

**Core extension:** optional `HouseholdEntityId` links the cohort to a Household legal entity so wages, dividends, and trades have a cash account. Documented as extension (SPEC cohort record has no entity id).

---

## 5. Labor capacity

Effective labor-hours (region) =

\[
\sum_c \mathrm{HouseholdCount}_c \times \mathrm{Hours}(\mathrm{LaborKind}_c) \times \mathrm{LaborQuality}_c
\]

Hours bands: Common 12 / Mean 18 / Extreme 24 per household-day.

**Capacity ≠ productivity.** Quality scales available hours; recipe labor hours encode technology intensity. Participation rate is omitted (SPEC allows). See `LaborSupply`.

---

## 6. Activities

`ProductionCalculator.ActualRuns` = floor of min(installed, space, labor, inputs).  
`ApplyRuns` debits inputs and credits outputs on the operator’s holdings in the activity region.

---

## 7–8. Resources & holdings

Catalog: `EconomyState.Resources` (Core extension for named kinds).  
Holdings keyed `Owner×Region×Resource` via `HoldingLedger` — upsert; no silent owner change.

---

## 9. Transport

**Carriage** moves location; **trade** changes owner (and usually pays). A transfer preserves `Owner` unless a sale step reassigns it.

`TransferEngine.StartTransfer` debits origin (logistics + lane capacity).  
`TickAndComplete` decrements `RemainingPeriods` and credits destination.

---

## 10. Shares

`ShareClass` + `ShareHolding(Units)`. Consistency: Σ held units + treasury = issued (`ShareMath.IsConsistent`). Households cannot be issuers.

---

## 11–13. Loans, credit, obligations

- Loans: Performing / Delinquent / Defaulted / Repaid; interest → obligations; lender asset / borrower liability symmetry.
- Credit facilities: `Available = Limit − Drawn`; only **committed** undrawn counts in liquidity; draw creates/augments a loan.
- Obligations: kinds + statuses; settle Wage → Tax → Interest → Principal → … (`ObligationEngine`).

---

## 14. Liquidity vs solvency (Minsky)

`LiquidityPosition`: cash + accessible deposits + undrawn **committed** credit − due-now obligations.  
`Liquidity.SimpleSolvency`: stock claim on net worth (cash + deposits + undrawn − loans). Illiquid ≠ insolvent; insolvent ≠ immediately illiquid — BM tracks both simply.

---

## 15. Banks & endogenous money

Non-bank lend: cash transfer (portfolio shift).  
Bank lend: **+loan asset +deposit liability** (post-Keynesian / endogenous money). Settlement from deposits can reassign deposit claims without vault cash (`DepositLedger.TryPayFromDeposits`).

---

## 16–17. Insurance & state policy

Premiums → obligations; pending `LossEvent`s → claim obligations.  
`StatePolicy` tax/transfer flows are money-conserving (State cash ↔ household/firm).

---

## 18. Stocks vs flows (Godley & Lavoie)

Stocks live on `EconomyState`. Flows accumulate on `PeriodFlowLedger` during the period (money created, cash moved, taxes, wages, production counters). Reconcile step asserts ownership/finance invariants — SFC spirit: every flow has counterparties; period opens/closes consistently.

Contrast: Arrow–Debreu GE clears all markets simultaneously; BM is a **sequential** period machine with rationing.

---

## 19. Invariants

`InvariantChecker.Check` / `AssertAll`: cash/holdings non-negative, share unit consistency, facility draw ≤ limit, living/production capacity, deposit banks are banks, household unownable as share issuer.

---

## 20. Period pipeline

`DefaultPeriodPipeline.Create()` — sixteen mechanisms in SPEC order; `EconomyEngine.Advance` folds them. Demand uses **posted unit prices + quantity rationing** (no order book).

---

## 21. Aggregate state

`EconomyState` matches SPEC §21 fields, plus Core extensions: `Resources`, `Lanes`, `PostedPrices`, `PendingLosses`, `Flows`, `Scratch`, and dictionary-keyed holdings/share classes for O(1) upsert.

---

## Non-goals (SPEC §22)

| Excluded | Why |
|---|---|
| Order books | Matching is posted-price rationing |
| Individual workers / vehicles | Aggregation is the BM point |
| Complete markets / GE clearing | Sequential stock–flow, not AD |
| NearSol / Logistics weave | Separate pass |

---

## Economic grammar (summary)

Ownership, location, and claims are **stocks**. Production, trade, carriage, lending, tax, and settlement are **flows**. Banks create deposits when they lend; lenders reshuffle cash. Liquidity is due-now coverage; solvency is net claims. Capacity binds living, production space, and logistics. Households are unownable; firms issue unit shares.

### Citations

- Godley, W. & Lavoie, M. — *Monetary Economics* (stock–flow consistent accounting).
- Minsky, H. — financial instability; liquidity vs solvency.
- Tobin, J. — portfolio balance; financial claims as assets.
- Post-Keynesian / endogenous money literature — bank loan creates deposit.
- Arrow–Debreu — contrast only; BM is not GE clearing of everything.

---

## Usage

```csharp
using Novolis.Economy.Core.Extensions;
using Novolis.Economy.Core.Steps;

var engine = DefaultPeriodPipeline.CreateEngine();
var next = engine.Advance(state);
var snap = next.Snapshot();                 // macro stocks / counts
var firm = next.InsightFor(firmId);         // liquidity vs solvency
var regions = next.RegionInsights();        // capacity utilization
```

Tests: `tests/Novolis.Economy.Unit` (`EconomyCore*`).

Validation scenarios (`EconomyCoreValidationScenariosTests`): Godley–Lavoie-style cash conservation without bank money; Graziani monetary circuit (create → wage → spend → repay/destroy); Minsky illiquid-but-solvent; posted-price quantity rationing; living/logistics capacity binds.

Over-time behaviours (`EconomyCoreOverTimeTests`): multi-period cash conservation; carriage lag; delinquency→default aging; production accumulation; bank interest deposit drain; fiscal transfer exhaustion; Minsky stress across periods.

## Install

```bash
dotnet add package Novolis.Economy.Core
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`).


