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
