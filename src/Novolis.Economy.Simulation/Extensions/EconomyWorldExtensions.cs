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

/// <summary>Ops-layer report section (FirmLedger / logistics / inventory — not Core stocks).</summary>
public sealed record WorldOpsReport(
    LedgerBookSnapshot Ledgers,
    LoanBookSnapshot Loans,
    LogisticsSnapshot Logistics,
    InventorySnapshot Inventory,
    MarketBookSnapshot Markets,
    IReadOnlyList<ConsumerCohortInsight> Cohorts);

/// <summary>Core-layer report section (vault cash / deposits / BM books). Never sum with Ops cash.</summary>
public sealed record WorldCoreReport(
    EconomySnapshot Snapshot,
    PeriodFlowInsight Flows,
    ObligationBookInsight Obligations,
    CreditBookInsight Credit);

/// <summary>
/// Nested world report. Ops and Core are separate money truths —
/// formatters must label them and must not add Ops cash + Core cash.
/// </summary>
public sealed record WorldReportSnapshot(
    WorldOpsReport Ops,
    WorldCoreReport? Core);

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

/// <summary>Short labeled formatter — never merges Ops and Core cash.</summary>
public static class WorldReportFormatter
{
    /// <summary>Formats a nested world report without summing Ops and Core cash.</summary>
    public static string Format(WorldReportSnapshot report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("EconomyWorld report");
        sb.AppendLine(new string('-', 40));

        var ops = report.Ops;
        sb.AppendLine("Ops");
        sb.AppendLine(CultureInfo.InvariantCulture,
            $"  firms {ops.Ledgers.FirmCount}  Ops cash {Fmt(ops.Ledgers.OpsTotalCash)}");
        sb.AppendLine(CultureInfo.InvariantCulture,
            $"  invoice AR {Fmt(ops.Ledgers.InvoiceOpenReceivables)}  ledger AR {Fmt(ops.Ledgers.LedgerAccountsReceivable)}  " +
            $"invoices open/settled {ops.Ledgers.OpenInvoiceCount}/{ops.Ledgers.SettledInvoiceCount}");
        sb.AppendLine(CultureInfo.InvariantCulture,
            $"  loans active/defaulted/closed {ops.Loans.ActiveCount}/{ops.Loans.DefaultedCount}/{ops.Loans.ClosedCount}  " +
            $"principal {Fmt(ops.Loans.PrincipalOutstanding)}");
        sb.AppendLine(CultureInfo.InvariantCulture,
            $"  shipments {ops.Logistics.ShipmentCount}  cargo in flight {ops.Logistics.CargoQuantityInFlight:0.####}  " +
            $"corridor toll exposure {Fmt(ops.Logistics.CorridorTollExposure)}");
        sb.AppendLine(CultureInfo.InvariantCulture,
            $"  inventory slots {ops.Inventory.SlotCount}  qty {ops.Inventory.TotalQuantity:0.####}  book cost {Fmt(ops.Inventory.TotalBookCost)}");
        sb.AppendLine(CultureInfo.InvariantCulture,
            $"  market products {ops.Markets.ProductCount}  trades {ops.Markets.TotalTrades}");
        sb.AppendLine(CultureInfo.InvariantCulture,
            $"  cohorts {ops.Cohorts.Count}");

        if (report.Core is { } core)
        {
            sb.AppendLine("Core");
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"  Core cash {Fmt(core.Snapshot.TotalCash)}  deposits {Fmt(core.Snapshot.TotalDeposits)}  " +
                $"broad money {Fmt(core.Snapshot.BroadMoney)}");
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"  last-period net money {Fmt(core.Flows.NetMoneyCreated)}  " +
                $"loans principal {Fmt(core.Credit.LoanPrincipalOutstanding)}  " +
                $"undrawn {Fmt(core.Credit.UndrawnCommitted)}");
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"  obligations pending/delinq {core.Obligations.PendingCount}/{core.Obligations.DelinquentCount}  " +
                $"due now {Fmt(core.Obligations.DueNow)}");
        }
        else
        {
            sb.AppendLine("Core");
            sb.AppendLine("  (empty — no Core entities)");
        }

        return sb.ToString();
    }

    private static string Fmt(Money m) => m.Amount.ToString("0.####", CultureInfo.InvariantCulture);
}
