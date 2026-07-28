# Release

Packages publish as `Novolis.Economy.*` on GitHub Packages under the `2026.1.*` line.

## Policy

- [Release policy](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/release-policy.md)
- [Package policy](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/package-policy.md) - README packed per package, XML docs required

## Local validation

```bash
dotnet build Novolis.Economy.slnx
dotnet test Novolis.Economy.slnx
pwsh -File ../novolis-governance/scripts/verify-nuget-only.ps1
```

## Versioning

Bump versions via `build/version.json` / `build/version.props` per governance; consumers pin `2026.1.*` from GPR.
