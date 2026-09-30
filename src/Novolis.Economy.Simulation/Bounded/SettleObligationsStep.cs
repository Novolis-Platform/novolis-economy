using Novolis.Economy.Core;
using Novolis.Economy.Core.Finance;
using Novolis.Economy.Core.Holdings;
using Novolis.Economy.Core.Invariants;
using Novolis.Economy.Core.Labor;
using Novolis.Economy.Core.Production;
using Novolis.Economy.Core.Transport;
using Novolis.Economy.Core.Transactions;
using CoreLegalEntityKind = Novolis.Economy.Core.LegalEntityKind;

using Novolis.Economy.Simulation.Bounded;

namespace Novolis.Economy.Simulation.Bounded;

/// <summary>11. Settle obligations by liquidity and priority.</summary>
public sealed class SettleObligationsStep : IBoundedPeriodStep
{
    public string Name => "11_SettleObligations";

    public BoundedPeriodState Execute(BoundedPeriodState current) =>
        current.WithEconomy(ObligationEngine.SettleDue(current));
}
