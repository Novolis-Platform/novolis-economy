# Novolis.Economy.Population

Consumer cohort stubs. Demand is modeled per cohort, not per individual citizen.

`PopulationCount` is **household count** (no headcount layer). Labor =
`Households × HoursPerDay(Common=12 / Mean=18 / Extreme=24) / 24` per tick.
Cohorts carry `Productivity` and optional `HouseholdFirmId`.

```bash
dotnet add package Novolis.Economy.Population
```
