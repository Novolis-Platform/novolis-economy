<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-economy">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Economy.Finance

Inter-firm **term-loan behavior**: origination policy, interest convention,
repayment, and default decisions. Core owns the resulting financial claims;
Accounting observes them.

Agents (not ML) enqueue `OriginateLoan` / `RepayLoan` from `Novolis.Economy.Production`. Settlement runs in Simulation's `SettleFinance` phase via `LoanEngine`.

## Install

```bash
dotnet add package Novolis.Economy.Finance
```

Depends on `Novolis.Economy.Abstractions`, `Novolis.Economy.Core`, and
`Novolis.Economy.Production`. Finance does not depend on Accounting.

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
| `Novolis.Economy.Core` | Authoritative claims, obligations, and atomic settlement |
| `Novolis.Economy.Accounting` | Read-only financial projections and transaction explanations |
| `Novolis.Economy.Production` | `OriginateLoan`, `RepayLoan`, `LoanOriginated`, … events |
| `Novolis.Economy.Agents` | `TreasuryFirmAgent`, `HouseholdFirmAgent` enqueue loan commands |
| `Novolis.Economy.Simulation` | `SettleFinance` phase, `ICreditCirculationSource` impl |

