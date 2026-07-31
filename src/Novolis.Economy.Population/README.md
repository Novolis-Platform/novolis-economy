# Novolis.Economy.Population

**Consumer cohort** stubs. Demand is modeled per cohort, not per individual citizen.

`PopulationCount` is **household count** (no headcount layer). Labor =
`Households × HoursPerDay(Common=12 / Mean=18 / Extreme=24) / 24` per tick.
Cohorts carry `Productivity` and optional `HouseholdFirmId`.

## Install

```bash
dotnet add package Novolis.Economy.Population
```

## Quick start

```csharp
using Novolis.Economy;
using Novolis.Economy.Population;

var cohort = new ConsumerCohort(
  id, new PopulationCount(1_200),
  DisposableIncome: Money.From(500_000m),
  preferences, areaId,
  Productivity: HouseholdProductivityKind.Mean,
  HouseholdFirmId: householdFirmId);

var states = new List<CohortState> { new(cohort) };

DemandEngine.ResolvePurchases(
  states, retailPrices, retailFacilities, products,
  inventory, ledgers, hour, emit: e => { /* record IEconomyEvent */ },
  priceElasticity: 0.15m);
```

`CohortState.BudgetRemaining` resets at period boundaries (see Simulation `CohortBudgetResetMode`).

## API

| Type | Role |
|------|------|
| `ConsumerCohort` | Segment definition (population, income, prefs, area) |
| `CohortState` | Runtime cohort + `BudgetRemaining` |
| `PreferenceProfile` / `CategoryPreference` | Category weights and sensitivity stubs |
| `PopulationCount` | Household count wrapper |
| `HouseholdProductivityKind` | Common / Mean / Extreme hours band |
| `HouseholdProductivity` | Hours-per-day lookup |
| `HouseholdMath` | Budget and labor helpers |
| `DemandEngine` | Posted-price retail clearing across cohorts |
| `ConsumerCohortExtensions` | World / insight helpers |

## Related

| Package | Role |
|---------|------|
| `Novolis.Economy.Simulation` | Cohort living clamp, labor pools, budget reset |
| `Novolis.Economy.Markets` | Retail prices and observed tape |
| `Novolis.Economy.Agents` | `HouseholdFirmAgent` spends above comfort |
| `Novolis.Economy.Production` | `GoodsSold` events from cohort purchases |
