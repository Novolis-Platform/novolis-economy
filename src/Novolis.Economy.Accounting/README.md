# Novolis.Economy.Accounting

Double-entry **ledgers**, **invoices**, period-close markers, and the **ownership engine** (dividends, claim assign/transfer helpers, capacity investment posts).

Owns the ops `OwnershipClaim` DTO (Core share holdings remain the BM ownership model in `Novolis.Economy.Core`).

## Install

```bash
dotnet add package Novolis.Economy.Accounting
```

## Quick start

```csharp
using Novolis.Economy.Accounting;

var ledger = new FirmLedger(firmId);
ledger.SeedCash(Money.From(50_000m), SimulationDate.Epoch);

LedgerEngine.PostCashSale(ledger, revenue: Money.From(120m), cogs: Money.From(80m), hour.Date);
OwnershipEngine.TryAssign(claims, issuer, owner, fraction: 0.25m, canIssueShares);
```

Loan and transport posts (`PostLoanDisbursement`, `PostInterestAccrual`, fuel/toll expenses) are called from Finance and Logistics settlement.

## API

| Type | Role |
|------|------|
| `FirmLedger` | Per-firm chart, balances, `Post`, `SeedCash` / `SeedInventory` |
| `AccountRole` | Cash, Inventory, Revenue, COGS, Notes Receivable/Payable, … |
| `LedgerEntry` / `LedgerSide` | Double-entry line |
| `LedgerEngine` | Cash sale/purchase, wages, spoilage, loan, transport posts |
| `FirmLedgerExtensions` / `LedgerBookExtensions` | Bulk helpers |
| `Invoice` | Open commercial invoice with remaining balance |
| `OwnershipEngine` | `TryAssign`, `TryTransfer`, dividends, facility upgrade posts |
| `OwnershipClaim` | Fractional claim on an issuer firm (ops DTO) |

## Related

| Package | Role |
|---------|------|
| `Novolis.Economy.Finance` | `LoanEngine` posts through `LedgerEngine` |
| `Novolis.Economy.Production` | Sales / procurement events drive ledger entries |
| `Novolis.Economy.Logistics` | Fuel bunkering and toll expense accounts |
| `Novolis.Economy.Simulation` | Holds firm ledger collection on `EconomyWorld` |
