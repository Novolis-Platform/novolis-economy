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

/// <summary>6. Resolve household and firm demand budgets (scratch only).</summary>
public sealed class ResolveDemandStep : IBoundedPeriodStep
{
    public string Name => "06_ResolveDemand";

    public BoundedPeriodState Execute(BoundedPeriodState current) => current;
}
