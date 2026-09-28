using System.Reflection;
using Novolis.Economy.Primitives;

namespace Novolis.Economy.Unit.Core;

public sealed class PrimitivesTests
{
    [Test]
    public async Task EconomicPosition_RepresentsAnAssetQuantity()
    {
        var owner = EconomicEntityId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var asset = EconomicAssetId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        var region = RegionId.From(Guid.Parse("33333333-3333-3333-3333-333333333333"));

        var position = new EconomicPosition(owner, asset, 100m, region);

        await Assert.That(position.Owner).IsEqualTo(owner);
        await Assert.That(position.Asset).IsEqualTo(asset);
        await Assert.That(position.Quantity).IsEqualTo(100m);
        await Assert.That(position.Region).IsEqualTo(region);
    }

    [Test]
    public async Task EconomicPosition_RejectsNegativeQuantity()
    {
        var act = () => new EconomicPosition(
            EconomicEntityId.New(),
            EconomicAssetId.New(),
            -1m);

        await Assert.That(act).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Primitives_HaveNoEconomyAssemblyReferences()
    {
        var references = typeof(EconomicPosition).Assembly
            .GetReferencedAssemblies()
            .Where(reference => reference.Name?.StartsWith("Novolis.Economy.", StringComparison.Ordinal) == true)
            .ToArray();

        await Assert.That(references).IsEmpty();
    }

    [Test]
    public async Task PrimitiveSurface_ContainsOnlyVocabularyTypes()
    {
        var forbidden = typeof(EconomicPosition).Assembly
            .GetTypes()
            .Where(type => type.Namespace?.Contains(".Core", StringComparison.Ordinal) == true)
            .ToArray();

        await Assert.That(forbidden).IsEmpty();
    }
}
