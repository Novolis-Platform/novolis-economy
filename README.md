<!-- novolis-package-index:start -->
> **GitHub Packages shows this repository README on every package page** (upstream limitation).
> Open the **package README** for install and quick start - embedded in each .nupkg and linked below.

## Published packages

| Package | Install | Package README |
|---------|---------|----------------|
| `Novolis.Economy` | `dotnet add package Novolis.Economy` | [README](https://github.com/Novolis-Platform/novolis-economy/blob/main/src/Novolis.Economy/README.md) |
| `Novolis.Economy.Production` | `dotnet add package Novolis.Economy.Production` | [README](https://github.com/Novolis-Platform/novolis-economy/blob/main/src/Novolis.Economy.Production/README.md) |
| `Novolis.Economy.Markets` | `dotnet add package Novolis.Economy.Markets` | [README](https://github.com/Novolis-Platform/novolis-economy/blob/main/src/Novolis.Economy.Markets/README.md) |
| `Novolis.Economy.Accounting` | `dotnet add package Novolis.Economy.Accounting` | [README](https://github.com/Novolis-Platform/novolis-economy/blob/main/src/Novolis.Economy.Accounting/README.md) |
| `Novolis.Economy.Logistics` | `dotnet add package Novolis.Economy.Logistics` | [README](https://github.com/Novolis-Platform/novolis-economy/blob/main/src/Novolis.Economy.Logistics/README.md) |
| `Novolis.Economy.Population` | `dotnet add package Novolis.Economy.Population` | [README](https://github.com/Novolis-Platform/novolis-economy/blob/main/src/Novolis.Economy.Population/README.md) |
| `Novolis.Economy.Simulation` | `dotnet add package Novolis.Economy.Simulation` | [README](https://github.com/Novolis-Platform/novolis-economy/blob/main/src/Novolis.Economy.Simulation/README.md) |

For NuGet.org and Visual Studio, the **embedded** README.md inside each package is authoritative.

<!-- novolis-package-index:end -->

# novolis-economy

**Headless economic simulation libraries** — products, markets, accounting, logistics, population cohorts, and a deterministic phase runner.

Not a game engine and not spatial simulation. Product hosts (for example a future Novolis Commerce app) compose these packages.

## Build

```powershell
dotnet build Novolis.Economy.slnx
dotnet test Novolis.Economy.slnx
dotnet pack Novolis.Economy.slnx -c Release -o artifacts/packages
```

Packages are published to **GitHub Packages** on merge to `main` (nuget.org + github restore only).

## Policy

See [library-boundaries.md](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/library-boundaries.md) for Math/Physics/Simulation. Economy is an orthogonal domain family documented in [docs/design.md](docs/design.md).
