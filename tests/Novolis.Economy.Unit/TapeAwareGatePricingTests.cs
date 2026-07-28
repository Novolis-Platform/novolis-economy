using Novolis.Economy;
using Novolis.Economy.Markets;
using TUnit.Core;

namespace Novolis.Economy.Unit;

public sealed class TapeAwareGatePricingTests
{
  private static readonly ProductId Ore = ProductId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a1"));

  [Test]
  public async Task Empty_Tape_Returns_Floor()
  {
    var book = new ObservedMarketBook();
    await Assert.That(TapeAwareGatePricing.Gate(book, Ore, floor: 10m)).IsEqualTo(10m);
  }

  [Test]
  public async Task Rising_Trend_Gate_Exceeds_Falling_And_Stable_Undercut()
  {
    const decimal floor = 10m;
    const decimal last = 20m;

    var rising = new ObservedMarketBook();
    rising.RecordTrade(Ore, Quantity.From(1m), Money.From(15m), new SimulationHour(0));
    rising.RecordTrade(Ore, Quantity.From(1m), Money.From(last), new SimulationHour(1));

    var falling = new ObservedMarketBook();
    falling.RecordTrade(Ore, Quantity.From(1m), Money.From(25m), new SimulationHour(0));
    falling.RecordTrade(Ore, Quantity.From(1m), Money.From(last), new SimulationHour(1));

    var flat = new ObservedMarketBook();
    flat.RecordTrade(Ore, Quantity.From(1m), Money.From(last), new SimulationHour(0));
    flat.RecordTrade(Ore, Quantity.From(1m), Money.From(last), new SimulationHour(1));

    await Assert.That(rising.Trend(Ore)).IsEqualTo(MarketTrend.Rising);
    await Assert.That(falling.Trend(Ore)).IsEqualTo(MarketTrend.Falling);
    await Assert.That(flat.Trend(Ore)).IsEqualTo(MarketTrend.Stable);

    var riseGate = TapeAwareGatePricing.Gate(rising, Ore, floor);
    var flatGate = TapeAwareGatePricing.Gate(flat, Ore, floor);
    var fallGate = TapeAwareGatePricing.Gate(falling, Ore, floor);

    await Assert.That(riseGate).IsGreaterThanOrEqualTo(flatGate);
    await Assert.That(flatGate).IsGreaterThanOrEqualTo(fallGate);
  }
}
