<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-economy">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Economy.Production

Product **recipes**, quality-bearing **batches**, facility workflow layout stubs, and the shared **command / event** vocabulary for the ops economy stack.

Owns `InventoryStore`, production throttling, and hub-order commands consumed by Simulation phases and Agents.

## Install

```bash
dotnet add package Novolis.Economy.Production
```

Foundation package for most other `Novolis.Economy.*` ops libraries.

## Quick start — production plan

```csharp
using Novolis.Economy;
using Novolis.Economy.Production;

sim.Enqueue(new SetProductionPlan(firmId, facilityId, productId, plannedUnits: Quantity.From(100m)));
await sim.AdvanceAsync(SimulationDuration.FromHours(1));
// ApplyProduction phase calls ProductionEngine.TryProduce
```

Register a recipe and throttle by stock:

```csharp
var rate = ProductionThrottle.Rate(baseRate: 10m, onHand: 85m, targetOnHand: 100m, floorRate: 2m);
```

## API

| Type | Role |
|------|------|
| `ProductDefinition` | Recipe inputs, attributes, process, optional shelf life |
| `ProductBatch` | Lot with quantity, quality, unit cost, produced date |
| `ProductInput` / `ProductQuality` | Recipe line and quality score |
| `OperatingUnitKind` | Facility node kinds (Purchasing, Manufacturing, Sales, …) |
| `FacilityLayout` | Operating units + material routes graph |
| `InventoryStore` | Firm × location × product batches |
| `ProductionEngine` | `TryProduce` — inputs, capacity, labor limits |
| `ProductionThrottle` | Overstock taper on planned rate |
| `DeterministicRandom` | Seeded RNG for agents and simulation |
| `IEconomyCommand` / `IEconomyEvent` / `IEconomyProjection` | Marker interfaces for queue and telemetry |
| Commands | `SetProductionPlan`, `PostHubOrder`, `OriginateLoan`, `IssueShipment`, … |
| Events | `BatchProduced`, `GoodsSold`, `HubOrderFilled`, `LoanOriginated`, … |

## Dogfooding / apps

Production plans, inventory, and hub trades drive [`novolis-dogfooding`](https://github.com/Novolis-Platform/novolis-dogfooding) economy scenarios.

## Related

| Package | Role |
|---------|------|
| `Novolis.Economy.Core` | BM kernel; period-close authority |
| `Novolis.Economy.Simulation` | Applies commands in ordered phases |
| `Novolis.Economy.Markets` | Hub book + pricing on top of production events |
| `Novolis.Economy.Accounting` | Ledger posts triggered by production / sales events |

