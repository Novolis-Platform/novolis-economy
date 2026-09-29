<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-economy">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Economy.Accounting

Read-only financial observability over `Novolis.Economy.Core`, plus
compatibility adapters for the older operational ledger and ownership APIs.
Accounting does not own cash, inventory, claims, or settlement.

Owns the ops `OwnershipClaim` DTO (Core share holdings remain the BM ownership model in `Novolis.Economy.Core`).

## Install

```bash
dotnet add package Novolis.Economy.Accounting
```

## Financial projections

```csharp
using Novolis.Economy.Accounting;

var firmFinancials = AccountingQuery.Project(
    simulation.State.World.CoreState,
    new FinancialScope.Entity(firmId.AsCore()));

var regionalFinancials = AccountingQuery.Project(
    simulation.State.World.CoreState,
    new FinancialScope.Region(regionId));

Console.WriteLine(firmFinancials.NetWorth);
```

`FinancialScope` also supports household cohorts, entity kinds, and explicit
groups. The resulting balance sheets and transaction list are derived from
Core state and its journal. Calling a projection never mutates the economy.

## API

| Type | Role |
|------|------|
| `AccountingQuery` | Pure read-side projection query |
| `FinancialProjection` | Scope totals, entity books, liquidity, claims, and explanations |
| `FinancialScope` | Entity, cohort, region, kind, and explicit group selection |
| `FirmLedger` | Compatibility chart for older operational hosts |
| `AccountRole` | Cash, Inventory, Revenue, COGS, Notes Receivable/Payable, … |
| `LedgerEntry` / `LedgerSide` | Double-entry line |
| `LedgerEngine` | Compatibility posting adapter; Core remains authoritative |
| `FirmLedgerExtensions` / `LedgerBookExtensions` | Bulk helpers |
| `Invoice` | Open commercial invoice with remaining balance |
| `OwnershipEngine` | Compatibility ownership/report adapter |
| `OwnershipClaim` | Fractional claim on an issuer firm (ops DTO) |

## Related

| Package | Role |
|---------|------|
| `Novolis.Economy.Core` | Authoritative positions, claims, transactions, and invariants |
| `Novolis.Economy.Finance` | Credit behavior and terms; does not depend on Accounting |
| `Novolis.Economy.Production` | Sales / procurement events drive ledger entries |
| `Novolis.Economy.Logistics` | Fuel bunkering and toll expense accounts |
| `Novolis.Economy.Simulation` | Runs the model and captures financial observations |

