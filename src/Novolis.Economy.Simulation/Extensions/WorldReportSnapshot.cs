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

/// <summary>
/// Nested world report. Ops and Core are separate money truths —
/// formatters must label them and must not add Ops cash + Core cash.
/// </summary>
public sealed record WorldReportSnapshot(
    WorldOpsReport Ops,
    WorldCoreReport? Core);
