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

/// <summary>8. Transfer ownership and payments for matched trades (quantity rationing; no order book).</summary>
public sealed class TransferOwnershipPaymentsStep : IBoundedPeriodStep
{
    public string Name => "08_TransferOwnershipPayments";

    public BoundedPeriodState Execute(BoundedPeriodState current)
    {
        var state = current;
        foreach (var cohort in state.Cohorts.Values)
        {
            if (cohort.HouseholdEntityId is not { } buyerId || !state.Entities.ContainsKey(buyerId))
                continue;

            var availableCash = cohort.HouseholdEntityId is { } linkedHousehold &&
                                state.Entities.ContainsKey(linkedHousehold)
                ? CashLedger.Balance(state, linkedHousehold).Amount
                : HouseholdMath.TotalCash(cohort).Amount;
            var budget = Money.From(
                availableCash * Math.Clamp(cohort.Profile.ConsumptionWeight, 0m, 1m));
            budget = Money.From(Math.Min(budget.Amount, CashLedger.Balance(state, buyerId).Amount));

            foreach (var price in state.PostedPrices.Values.Where(p => p.RegionId.Equals(cohort.RegionId)))
            {
                if (budget.Amount <= 0m || price.UnitPrice.Amount <= 0m)
                    continue;
                if (!state.Resources.TryGetValue(price.ResourceId, out var res) ||
                    res.Kind != ResourceKind.ConsumerGood)
                    continue;

                var sellers = PositionLedger.ResourceView(state)
                    .Where(h => h.RegionId.Equals(cohort.RegionId) &&
                                h.ResourceId.Equals(price.ResourceId) &&
                                h.Quantity > 0m &&
                                !h.Owner.Equals(buyerId) &&
                                state.Entities.TryGetValue(h.Owner, out var e) &&
                                e.Kind == CoreLegalEntityKind.Firm)
                    .OrderByDescending(h => h.Quantity)
                    .ToList();

                foreach (var holding in sellers)
                {
                    if (budget.Amount <= 0m)
                        break;
                    var maxByCash = budget.Amount / price.UnitPrice.Amount;
                    var qty = Math.Min(holding.Quantity, maxByCash);
                    if (qty <= 1e-12m)
                        continue;
                    var cost = Money.From(qty * price.UnitPrice.Amount);
                    try
                    {
                        var asset = state.AssetFor(price.ResourceId);
                        state = state.WithEconomy(
                            CashLedger.EnsurePosition(state, buyerId));
                        state = state.WithEconomy(
                            CashLedger.EnsurePosition(state, holding.Owner));
                        state = state.WithEconomy(EconomicTransactionEngine.Apply(
                            state,
                            EconomicTransaction.Create(
                                state,
                                [
                                    new PositionChange(
                                        EconomicIdentity.For(holding.Owner),
                                        asset,
                                        -qty,
                                        cohort.RegionId),
                                    new PositionChange(
                                        EconomicIdentity.For(buyerId),
                                        asset,
                                        qty,
                                        cohort.RegionId),
                                    new PositionChange(
                                        EconomicIdentity.For(buyerId),
                                        state.MonetaryAssetId,
                                        -cost.Amount,
                                        Region: null),
                                    new PositionChange(
                                        EconomicIdentity.For(holding.Owner),
                                        state.MonetaryAssetId,
                                        cost.Amount,
                                        Region: null)
                                ],
                                "posted-price-purchase")));
                        state = state.WithEconomy(PositionLedger.UpdateLegacyResourceProjection(
                            state,
                            holding.Owner,
                            cohort.RegionId,
                            price.ResourceId,
                            holding.Quantity - qty));
                        state = state.WithEconomy(PositionLedger.UpdateLegacyResourceProjection(
                            state,
                            buyerId,
                            cohort.RegionId,
                            price.ResourceId,
                            PositionLedger.GetQuantity(
                                state,
                                EconomicIdentity.For(buyerId),
                                cohort.RegionId,
                                asset)));
                        budget = budget - cost;
                        state = state.WithFlows(state.Flows.RecordCashMoved(cost));
                    }
                    catch (InvalidOperationException)
                    {
                        // skip
                    }
                }
            }
        }

        return state;
    }
}
