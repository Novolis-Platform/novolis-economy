# Novolis.Economy.Markets

**Imperfect market intelligence** — firms never see ground truth directly. Observed trade tape, estimate stubs, and pricing helpers for hub orders and retail.

No order books or continuous double auctions; clearing uses posted prices + quantity rationing (see `Novolis.Economy.Core`).

## Install

```bash
dotnet add package Novolis.Economy.Markets
```

## Quick start — observed tape + gate price

```csharp
using Novolis.Economy.Markets;

var book = world.MarketBook;
book.RecordTrade(productId, quantity, unitPrice, sim.State.Clock);

var gate = TapeAwareGatePricing.Gate(book, productId, floor: 12.50m);
sim.Enqueue(new PostHubOrder(firmId, locationId, productId, HubOrderSide.Buy, qty, Money.From(gate)));
```

Inventory-pressure retail adjustment:

```csharp
var adjusted = InventoryPressurePricing.Adjust(
  basePrice, onHand: 40m, targetOnHand: 100m, maxPremium: 0.25m, maxDiscount: 0.25m);
```

## Quick start — intelligence stub

```csharp
IMarketIntelligenceService intel = new NullMarketIntelligenceService();
var est = intel.Estimate(firmId, MarketMetric.Demand, areaId);
// PointEstimate = 0, Uncertainty = 100% until a host replaces the service
```

## API

| Type | Role |
|------|------|
| `MarketMetric` | `Demand`, `Supply`, `AveragePrice`, `OwnMarketShare` |
| `MarketTrend` | `Unknown`, `Rising`, `Stable`, `Falling` |
| `MarketEstimate` | Imperfect point estimate + uncertainty band |
| `ProductMarketView` | Product-level projection for UI (`IEconomyProjection`) |
| `IMarketIntelligenceService` | `Estimate(firm, metric, area)` |
| `NullMarketIntelligenceService` | Zero-estimate skeleton |
| `ObservedMarketBook` | Trade tape per product; `RecordTrade`, `Trend` |
| `HubOrder` | Limit order on the hub book |
| `InventoryPressurePricing` | Stock-scarce premium / overstock discount |
| `TapeAwareGatePricing` | Blend last trade into bid gate price |
| `ObservedMarketBookExtensions` | Snapshot / insight helpers |

## Dogfooding / apps

Hub order quotes and tape-aware pricing are exercised by economy agents and dogfood sims in [`novolis-dogfooding`](https://github.com/Novolis-Platform/novolis-dogfooding) `apps/economy/`.

## Related

| Package | Role |
|---------|------|
| `Novolis.Economy.Production` | `PostHubOrder`, `MarketTradeObserved` |
| `Novolis.Economy.Agents` | Agents cancel/repost hub orders each tick |
| `Novolis.Economy.Population` | Cohort demand clearing at posted retail prices |
