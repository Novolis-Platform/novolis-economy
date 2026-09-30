using Novolis.Economy.Abstractions;
using Novolis.Economy.Core;
using Novolis.Economy.Core.Finance;
using Novolis.Economy.Core.Holdings;
using Novolis.Economy.Core.Invariants;
using Novolis.Economy.Core.Production;
using Novolis.Economy.Core.Transactions;
using Novolis.Economy.Primitives;

namespace Novolis.Economy.Models.SmallOpenRegionalTrade;

/// <summary>Request for a bounded working-capital draw from the model bank.</summary>
public sealed record DrawWorkingCapitalCommand(decimal Amount) : IEconomicModelCommand
{
    public string Kind => "draw-working-capital";
}
