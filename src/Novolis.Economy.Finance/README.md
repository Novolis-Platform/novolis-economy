# Novolis.Economy.Finance

Inter-firm **term loans**: originate, hourly interest accrual onto notes, repayment, and default at term when cash is insufficient.

Agents (not ML) enqueue `OriginateLoan` / `RepayLoan` from `Novolis.Economy.Production`. Settlement runs in Simulation's `SettleFinance` phase via `LoanEngine`.

## Install

```bash
dotnet add package Novolis.Economy.Finance
```

Depends on `Novolis.Economy.Accounting` and `Novolis.Economy.Production`.

## Quick start

```csharp
using Novolis.Economy;
using Novolis.Economy.Finance;

sim.Enqueue(new OriginateLoan(
  lenderFirmId, borrowerFirmId,
  Money.From(10_000m), annualInterestRate: 0.12m, termHours: 720));

await sim.AdvanceAsync(SimulationDuration.FromHours(24));
// SettleFinance phase calls LoanEngine.AccrueHour / TryRepay
```

Household lenders use `LoanEngine.TryOriginateHouseholdLender` (budget validated by caller). `ICreditCirculationSource` exposes loan aggregates to diagnostics without a Finance↔Simulation cycle.

## API

| Type | Role |
|------|------|
| `Loan` | Term loan contract (principal, rate, due hour, status) |
| `LoanStatus` | `Active`, `Defaulted`, `Closed` |
| `LoanEngine` | `TryOriginate`, `TryOriginateHouseholdLender`, `AccrueHour`, `TryRepay` |
| `LoanBookExtensions` | Query helpers on world loan collections |
| `ICreditCirculationSource` | Liquid stock, principal outstanding, credit-frozen counts (implemented in Simulation) |

## Related

| Package | Role |
|---------|------|
| `Novolis.Economy.Accounting` | `LedgerEngine.PostLoanDisbursement`, interest / repayment posts |
| `Novolis.Economy.Production` | `OriginateLoan`, `RepayLoan`, `LoanOriginated`, … events |
| `Novolis.Economy.Agents` | `TreasuryFirmAgent`, `HouseholdFirmAgent` enqueue loan commands |
| `Novolis.Economy.Simulation` | `SettleFinance` phase, `ICreditCirculationSource` impl |
