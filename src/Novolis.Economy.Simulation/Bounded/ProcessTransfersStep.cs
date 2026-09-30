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

/// <summary>9. Start pending transfers (already queued) and tick/complete in-flight ones.</summary>
public sealed class ProcessTransfersStep : IBoundedPeriodStep
{
    public string Name => "09_ProcessTransfers";

    public BoundedPeriodState Execute(BoundedPeriodState current) =>
        current.WithEconomy(TransferEngine.TickAndComplete(current));
}
