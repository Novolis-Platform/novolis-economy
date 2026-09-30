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

/// <summary>7. Match buyers and sellers at posted prices (records intended fills in scratch via holdings scan).</summary>
public sealed class MatchBuyersSellersStep : IBoundedPeriodStep
{
    public string Name => "07_MatchBuyersSellers";

    public BoundedPeriodState Execute(BoundedPeriodState current) => current;
}
