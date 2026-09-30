using System.Globalization;
using System.Text;
using Novolis.Economy.Accounting.Extensions;
using Novolis.Economy.Core.Extensions;
using Novolis.Economy.Finance.Extensions;
using Novolis.Economy.Logistics.Extensions;
using Novolis.Economy.Markets.Extensions;
using Novolis.Economy.Population.Extensions;
using Novolis.Economy.Production.Extensions;

namespace Novolis.Economy.Simulation.Extensions;

/// <summary>Build nested report snapshots from an <see cref="EconomyWorld"/>.</summary>
public static class EconomyWorldExtensions
{
    /// <summary>Nested Ops + optional Core report snapshot.</summary>
    public static WorldReportSnapshot ToReportSnapshot(this EconomyWorld world)
    {
        var ops = new WorldOpsReport(
            Ledgers: ((IReadOnlyDictionary<FirmId, Accounting.FirmLedger>)world.Ledgers)
                .Snapshot(world.Invoices),
            Loans: world.Loans.Snapshot(),
            Logistics: world.Shipments.Snapshot(world.Hubs, world.Corridors),
            Inventory: world.Inventory.Snapshot(),
            Markets: world.MarketBook.Snapshot(),
            Cohorts: world.Cohorts.Select(c => c.ToInsight()).ToList());

        WorldCoreReport? core = null;
        if (world.CoreState.Entities.Count > 0)
        {
            var state = world.CoreState;
            core = new WorldCoreReport(
                Snapshot: state.Snapshot(),
                Flows: state.FlowInsight(),
                Obligations: state.ObligationBook(),
                Credit: state.CreditBook());
        }

        return new WorldReportSnapshot(ops, core);
    }
}
