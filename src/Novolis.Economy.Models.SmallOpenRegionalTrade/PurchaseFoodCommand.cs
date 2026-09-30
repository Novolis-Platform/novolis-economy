using Novolis.Economy.Abstractions;
using Novolis.Economy.Core;
using Novolis.Economy.Core.Finance;
using Novolis.Economy.Core.Holdings;
using Novolis.Economy.Core.Invariants;
using Novolis.Economy.Core.Production;
using Novolis.Economy.Core.Transactions;
using Novolis.Economy.Primitives;

namespace Novolis.Economy.Models.SmallOpenRegionalTrade;

/// <summary>Player or actor request for additional household food demand.</summary>
public sealed record PurchaseFoodCommand(decimal Quantity) : IEconomicModelCommand
{
    public string Kind => "purchase-food";
}
