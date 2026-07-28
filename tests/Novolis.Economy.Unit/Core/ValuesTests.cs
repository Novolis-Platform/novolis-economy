using Novolis.Economy;
using TUnit.Core;

namespace Novolis.Economy.Unit.Core;

public sealed class ValuesTests
{
  [Test]
  public async Task Money_Addition_PreservesSum()
  {
    var a = Money.From(10.5m);
    var b = Money.From(2.25m);
    await Assert.That((a + b).Amount).IsEqualTo(12.75m);
  }

  [Test]
  public async Task Quantity_Equality_UsesValue()
  {
    await Assert.That(Quantity.From(3m)).IsEqualTo(Quantity.From(3m));
  }

  [Test]
  public async Task Percentage_FromFraction_Scales()
  {
    await Assert.That(Percentage.FromFraction(0.184m).Value).IsEqualTo(18.4m);
  }

  [Test]
  public async Task SimulationDuration_RejectsNegativeHours()
  {
    var act = () => SimulationDuration.FromHours(-1);
    await Assert.That(act).Throws<ArgumentOutOfRangeException>();
  }

  [Test]
  public async Task DeterministicRandom_SameSeed_SameSequence()
  {
    var a = new DeterministicRandom(42);
    var b = new DeterministicRandom(42);
    await Assert.That(a.NextDouble()).IsEqualTo(b.NextDouble());
    await Assert.That(a.NextInt(100)).IsEqualTo(b.NextInt(100));
  }
}
