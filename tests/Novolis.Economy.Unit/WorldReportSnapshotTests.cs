using Novolis.Economy;
using Novolis.Economy.Simulation;
using Novolis.Economy.Simulation.Extensions;
using TUnit.Core;

namespace Novolis.Economy.Unit;

public sealed class WorldReportSnapshotTests
{
    [Test]
    public async Task ToReportSnapshot_LabelsOpsCash_WithoutCombinedTotal()
    {
        var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a1"));
        var world = new EconomyWorldBuilder(new EconomyPolicy())
            .AddFirm(firm, "Firm", Money.From(75m))
            .Build();

        var snap = world.ToReportSnapshot();
        await Assert.That(snap.Ops.Ledgers.OpsTotalCash.Amount).IsEqualTo(75m);
        await Assert.That(snap.Core).IsNull();

        var text = WorldReportFormatter.Format(snap);
        await Assert.That(text).Contains("Ops");
        await Assert.That(text).Contains("Ops cash");
        await Assert.That(text).Contains("(empty — no Core entities)");
        await Assert.That(text.Contains("combined", StringComparison.OrdinalIgnoreCase)).IsFalse();
        await Assert.That(text.Contains("Ops cash + Core", StringComparison.OrdinalIgnoreCase)).IsFalse();
    }
}
