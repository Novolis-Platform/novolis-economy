# Novolis.Economy.Logistics

Economic transport: hub/corridor/vehicle **models**, itinerary planning, and multi-leg shipments with fuel bunkering, dwell, berth capacity, crew labor, and tolls. `FreightRoute` remains a single-leg compatibility shim.

Owns `TransportHubId` / `TransportCorridorId` / `VehicleClassId`. Schedules carriage that completes into Core holdings via Simulation’s `CoreEconomyBridge`.

```bash
dotnet add package Novolis.Economy.Logistics
```
