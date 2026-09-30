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
