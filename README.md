<!-- novolis-marketing:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-brand-transparent.svg" width="360" alt="Novolis"/>
  </a>
</p>

<p align="center">
  <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/banners/novolis-economy.svg" width="100%" alt="novolis-economy"/>
</p>

<p align="center">
  <strong>Deterministic markets and firms</strong><br/>
  Headless economic simulation — supply chains, markets, accounting, logistics.
</p>

<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-economy/actions"><img src="https://img.shields.io/github/actions/workflow/status/Novolis-Platform/novolis-economy/merge.yml?branch=main&label=merge&logo=github" alt="merge"/></a>
  <a href="https://github.com/orgs/Novolis-Platform/packages?repo_name=novolis-economy"><img src="https://img.shields.io/badge/packages-GitHub%20Packages-0a7ea3?logo=nuget" alt="packages"/></a>
  <a href="https://github.com/Novolis-Platform"><img src="https://img.shields.io/badge/org-Novolis--Platform-111827" alt="org"/></a>
</p>

<p align="center">
  <a href="https://nuget.pkg.github.com/Novolis-Platform/index.json"><code>https://nuget.pkg.github.com/Novolis-Platform/index.json</code></a>
  ·
  <a href="https://github.com/Novolis-Platform/.github/blob/main/profile/README.md">Org landing</a>
  ·
  <a href="https://github.com/Novolis-Platform/novolis-governance">Governance</a>
</p>

---
<!-- novolis-marketing:end -->
<!-- novolis-package-index:start -->
> **GitHub Packages shows this repository README on every package page** (upstream limitation).
> Open the **package README** for install and quick start — embedded in each .nupkg and linked below.

## Published packages

| Package | Install | Package README |
|---------|---------|----------------|
| `Novolis.Economy.Accounting` | `dotnet add package Novolis.Economy.Accounting` | [README](https://github.com/Novolis-Platform/novolis-economy/blob/main/src/Novolis.Economy.Accounting/README.md) |
| `Novolis.Economy.Agents` | `dotnet add package Novolis.Economy.Agents` | [README](https://github.com/Novolis-Platform/novolis-economy/blob/main/src/Novolis.Economy.Agents/README.md) |
| `Novolis.Economy.Core` | `dotnet add package Novolis.Economy.Core` | [README](https://github.com/Novolis-Platform/novolis-economy/blob/main/src/Novolis.Economy.Core/README.md) |
| `Novolis.Economy.Finance` | `dotnet add package Novolis.Economy.Finance` | [README](https://github.com/Novolis-Platform/novolis-economy/blob/main/src/Novolis.Economy.Finance/README.md) |
| `Novolis.Economy.Logistics` | `dotnet add package Novolis.Economy.Logistics` | [README](https://github.com/Novolis-Platform/novolis-economy/blob/main/src/Novolis.Economy.Logistics/README.md) |
| `Novolis.Economy.Markets` | `dotnet add package Novolis.Economy.Markets` | [README](https://github.com/Novolis-Platform/novolis-economy/blob/main/src/Novolis.Economy.Markets/README.md) |
| `Novolis.Economy.Population` | `dotnet add package Novolis.Economy.Population` | [README](https://github.com/Novolis-Platform/novolis-economy/blob/main/src/Novolis.Economy.Population/README.md) |
| `Novolis.Economy.Production` | `dotnet add package Novolis.Economy.Production` | [README](https://github.com/Novolis-Platform/novolis-economy/blob/main/src/Novolis.Economy.Production/README.md) |
| `Novolis.Economy.Simulation` | `dotnet add package Novolis.Economy.Simulation` | [README](https://github.com/Novolis-Platform/novolis-economy/blob/main/src/Novolis.Economy.Simulation/README.md) |

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

