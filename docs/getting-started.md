# Getting started

`novolis-economy` ships NuGet packages for headless, deterministic economic simulation.

## Install

```bash
dotnet add package Novolis.Economy
dotnet add package Novolis.Economy.Simulation
```

Restore from GitHub Packages (`2026.1.*`) per [novolis-governance package policy](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/package-policy.md).

## Packages

| Package | Role |
|---------|------|
| `Novolis.Economy` | IDs, money/quantity, command/event/projection markers |
| `Novolis.Economy.Production` | Product and facility layout stubs |
| `Novolis.Economy.Markets` | Market intelligence stubs |
| `Novolis.Economy.Accounting` | Ledger stubs |
| `Novolis.Economy.Logistics` | Shipment and route stubs |
| `Novolis.Economy.Population` | Consumer cohort stubs |
| `Novolis.Economy.Simulation` | Phase pipeline and `IEconomySimulation` |

## Build and test

```bash
dotnet build Novolis.Economy.slnx
dotnet test Novolis.Economy.slnx
```

Pack (CI publishes to GitHub Packages on merge to `main`):

```powershell
dotnet pack Novolis.Economy.slnx -c Release -o artifacts/packages
```

## Next steps

- Package READMEs under `src/Novolis.Economy.*/README.md`
- [Design](design.md) for boundaries vs spatial Simulation
- [Release](release.md) for versioning and publishing
