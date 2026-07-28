using System.Collections.Immutable;
using Novolis.Economy;
using Novolis.Economy.Accounting;
using Novolis.Economy.Logistics;
using Novolis.Economy.Markets;
using Novolis.Economy.Population;
using Novolis.Economy.Production;
using TUnit.Core;

namespace Novolis.Economy.Unit.Smoke;

public sealed class ConstructibilityTests
{
  [Test]
  public async Task CoreTypes_AreConstructible()
  {
    var money = Money.From(1m);
    var qty = Quantity.From(2m);
    var explanation = MetricExplanation.Empty("ok", 1m);
    await Assert.That(money.Amount).IsEqualTo(1m);
    await Assert.That(qty.Value).IsEqualTo(2m);
    await Assert.That(explanation.Contributions.IsDefaultOrEmpty || explanation.Contributions.Length == 0).IsTrue();
  }

  [Test]
  public async Task ProductionTypes_AreConstructible()
  {
    var productId = ProductId.New();
    var batch = new ProductBatch(
      productId,
      Quantity.From(10m),
      new ProductQuality(80m),
      Money.From(1.2m),
      SimulationDate.Epoch,
      BrandId: null);
    var layout = new FacilityLayout(
      ImmutableDictionary<OperatingUnitId, OperatingUnit>.Empty,
      ImmutableArray<MaterialRoute>.Empty);
    await Assert.That(batch.Quantity.Value).IsEqualTo(10m);
    await Assert.That(layout.Units.Count).IsEqualTo(0);
  }

  [Test]
  public async Task MarketsAccountingLogisticsPopulation_AreConstructible()
  {
    var estimate = new NullMarketIntelligenceService()
      .Estimate(FirmId.New(), MarketMetric.Demand, GeographicAreaId.New());
    var entry = new LedgerEntry(
      Guid.NewGuid(),
      AccountId.New(),
      FirmId.New(),
      LedgerSide.Debit,
      Money.From(5m),
      SimulationDate.Epoch,
      Memo: null);
    var shipment = new Shipment(
      ShipmentId.New(),
      FreightRouteId.New(),
      ProductId.New(),
      Quantity.From(1m),
      SimulationHour.Epoch,
      SimulationHour.Epoch.AddHours(4),
      ShipmentStatus.Queued);
    var cohort = new ConsumerCohort(
      ConsumerCohortId.New(),
      new PopulationCount(1000),
      Money.From(50m),
      new PreferenceProfile(ImmutableArray<CategoryPreference>.Empty, 1m, 1m, 0.5m),
      GeographicAreaId.New());

    await Assert.That(estimate.PointEstimate).IsEqualTo(0m);
    await Assert.That(entry.Amount.Amount).IsEqualTo(5m);
    await Assert.That(shipment.Status).IsEqualTo(ShipmentStatus.Queued);
    await Assert.That(cohort.Population.Value).IsEqualTo(1000);
  }
}
