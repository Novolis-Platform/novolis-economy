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

/// <summary>Core-layer report section (vault cash / deposits / BM books). Never sum with Ops cash.</summary>
public sealed record WorldCoreReport(
    EconomySnapshot Snapshot,
    PeriodFlowInsight Flows,
    ObligationBookInsight Obligations,
    CreditBookInsight Credit);
