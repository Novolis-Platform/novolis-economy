# Novolis.Economy

**Primitives** package for the Economy family (PackageId remains `Novolis.Economy`).

Strong IDs (firms, products, loans, transport hubs/corridors/vehicles, …), `Money` / `Quantity` / `Percentage`, discrete simulation time, `LegalEntity` / `OwnershipClaim`, command/event/projection markers, metric explanations, and seeded RNG.

Domain engines and the Simulation composition root depend on this leaf. Claim **cash posting** lives in `Novolis.Economy.Accounting` (`OwnershipEngine`).

```bash
dotnet add package Novolis.Economy
```

See [docs/design.md](../../docs/design.md).
