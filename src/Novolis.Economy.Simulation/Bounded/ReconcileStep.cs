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

/// <summary>16. Reconcile stocks, claims, and ownership.</summary>
public sealed class ReconcileStep : IBoundedPeriodStep
{
    public string Name => "16_Reconcile";

    public BoundedPeriodState Execute(BoundedPeriodState current)
    {
        InvariantChecker.AssertAll(current);
        return current;
    }
}
