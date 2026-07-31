# Novolis.Economy.Logistics

Economic **transport**: hub/corridor/vehicle models, itinerary planning, and multi-leg shipments with fuel bunkering, dwell, berth capacity, crew labor, and tolls.

`FreightRoute` remains a single-leg compatibility shim. Owns `TransportHubId` / `TransportCorridorId` / `VehicleClassId`. Completed carriage credits Core holdings via Simulation's `CoreEconomyBridge`.

## Install

```bash
dotnet add package Novolis.Economy.Logistics
```

## Quick start — plan and issue shipment

```csharp
using Novolis.Economy;
using Novolis.Economy.Logistics;

sim.Enqueue(new PlanShipment(
  firmId, originHubId, destinationHubId, productId, quantity, vehicleClassId));
await sim.AdvanceAsync(SimulationDuration.FromHours(24));
// AdvanceLogistics phase ticks underway legs, bunkering, tolls
```

Estimate haul cost before committing:

```csharp
var estimate = HaulCostEstimator.Estimate(
  itinerary, corridors, vehicleClass, wageRatePerHour, fuelUnitCost);
```

## API

| Type | Role |
|------|------|
| `TransportHub` | Hub with inventory location, dwell hours, berth capacity |
| `TransportCorridor` | Directed leg (transit hours, max cargo, difficulty, toll) |
| `VehicleClass` | Cargo, fuel burn, crew labor, tank capacity |
| `Itinerary` | Ordered corridor sequence |
| `FreightRoute` | Legacy single-edge route shim |
| `ActiveShipment` / `ShipmentStatus` / `ShipmentPhase` | In-flight cargo state |
| `LogisticsEngine` | Depart, advance legs, bunker fuel, complete into inventory |
| `ItineraryPlanner` | Path find on hub network |
| `HaulCostEstimator` | Fuel + toll + labor estimate for an itinerary |
| `HullRiskQuotes` / `FtlDriveLifePolicy` | Carrier agent pricing helpers |
| `TransitProfile` / `TransitProfiles` | Product transit classification |
| `LogisticsNetworkExtensions` | Network query helpers |

## Dogfooding / apps

Multi-leg tramp freighter scenarios in [`novolis-dogfooding`](https://github.com/Novolis-Platform/novolis-dogfooding) `apps/economy/TrampFreighterPlay`.

## Related

| Package | Role |
|---------|------|
| `Novolis.Economy.Production` | `PlanShipment`, `IssueShipment`, shipment events |
| `Novolis.Economy.Simulation` | `AdvanceLogistics` phase, `CoreEconomyBridge` |
| `Novolis.Economy.Agents` | `CarrierFirmAgent` plans reposition / delivery |
| `Novolis.Economy.Accounting` | Transport fuel and toll ledger posts |
