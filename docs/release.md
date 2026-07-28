# Release

Packages publish as `Novolis.Economy.*` on GitHub Packages under the `2026.1.*` line.

## 2026.1.0 — Economic kernel

- `EconomyWorld` + `EconomyWorldBuilder`
- Working phase pipeline: production, logistics, demand, labor, ledger settlement
- Double-entry `FirmLedger` / `LedgerEngine`
- Commodity-chain scenario tests (determinism + ledger balance)

## Policy

- [Release policy](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/release-policy.md)
- [Package policy](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/package-policy.md)

## Local validation

```powershell
dotnet build Novolis.Economy.slnx -c Release
dotnet test --project tests/Novolis.Economy.Unit -c Release
dotnet pack Novolis.Economy.slnx -c Release -o artifacts/packages
```

## Versioning

Bump via `build/version.json` / `build/version.props` per governance; consumers pin `2026.1.*` from GPR.
