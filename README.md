<!-- novolis-package-index:start -->
> **GitHub Packages shows this repository README on every package page** (upstream limitation).
> Open the **package README** for install and quick start - embedded in each .nupkg and linked below.

## Published packages

| Package | Install | Role · [README](.) |
|---------|---------|-------------------|
| `Novolis.Economy.Core` | `dotnet add package Novolis.Economy.Core` | **BM economic kernel** · [README](https://github.com/Novolis-Platform/novolis-economy/blob/main/src/Novolis.Economy.Core/README.md) |
| `Novolis.Economy.Production` | `dotnet add package Novolis.Economy.Production` | Recipes, inventory, commands/events · [README](https://github.com/Novolis-Platform/novolis-economy/blob/main/src/Novolis.Economy.Production/README.md) |
| `Novolis.Economy.Markets` | `dotnet add package Novolis.Economy.Markets` | Observed tape, pricing, intelligence stubs · [README](https://github.com/Novolis-Platform/novolis-economy/blob/main/src/Novolis.Economy.Markets/README.md) |
| `Novolis.Economy.Accounting` | `dotnet add package Novolis.Economy.Accounting` | Ledgers, invoices, ownership engine · [README](https://github.com/Novolis-Platform/novolis-economy/blob/main/src/Novolis.Economy.Accounting/README.md) |
| `Novolis.Economy.Finance` | `dotnet add package Novolis.Economy.Finance` | Inter-firm term loans · [README](https://github.com/Novolis-Platform/novolis-economy/blob/main/src/Novolis.Economy.Finance/README.md) |
| `Novolis.Economy.Logistics` | `dotnet add package Novolis.Economy.Logistics` | Hub network, shipments, itineraries · [README](https://github.com/Novolis-Platform/novolis-economy/blob/main/src/Novolis.Economy.Logistics/README.md) |
| `Novolis.Economy.Population` | `dotnet add package Novolis.Economy.Population` | Consumer cohorts, retail demand · [README](https://github.com/Novolis-Platform/novolis-economy/blob/main/src/Novolis.Economy.Population/README.md) |
| `Novolis.Economy.Agents` | `dotnet add package Novolis.Economy.Agents` | Heuristic firm agents (not ML) · [README](https://github.com/Novolis-Platform/novolis-economy/blob/main/src/Novolis.Economy.Agents/README.md) |
| `Novolis.Economy.Simulation` | `dotnet add package Novolis.Economy.Simulation` | Tick runner, phases, world bridge · [README](https://github.com/Novolis-Platform/novolis-economy/blob/main/src/Novolis.Economy.Simulation/README.md) |

For NuGet.org and Visual Studio, the **embedded** README.md inside each package is authoritative.

<!-- novolis-package-index:end -->

# novolis-economy

**Headless economic simulation libraries** — **`Novolis.Economy.Core`** kernel → ops packages (Production, Logistics, …) → **Simulation** composition root (+ Agents).

**Breaking:** PackageId `Novolis.Economy` is retired. Replace `PackageReference Include="Novolis.Economy"` with `Novolis.Economy.Core` (see [docs/design.md](docs/design.md)).

Not a game engine and not spatial simulation. Product hosts compose these packages from GitHub Packages (`2026.1.*`).

## Build

```powershell
dotnet build Novolis.Economy.slnx
dotnet test Novolis.Economy.slnx
dotnet pack Novolis.Economy.slnx -c Release -o artifacts/packages
```

Packages are published to **GitHub Packages** on merge to `main` (nuget.org + github restore only).

Dogfood apps that consume those packages live in [`novolis-dogfooding`](https://github.com/Novolis-Platform/novolis-dogfooding) under `apps/economy/` (`EconomyBoard`, `TrampFreighterPlay`).

## Policy

See [library-boundaries.md](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/library-boundaries.md) for Math/Physics/Simulation. Economy is an orthogonal domain family documented in [docs/design.md](docs/design.md).
