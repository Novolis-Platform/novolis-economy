namespace Novolis.Economy.Core;

/// <summary>Supported valuation source for the initial Core implementation.</summary>
public enum ValuationMethod
{
    /// <summary>Use the currently posted regional resource price.</summary>
    PostedPrice = 0,
}

/// <summary>Context required to turn a position quantity into monetary value.</summary>
public sealed record ValuationContext(
    EconomicAssetId UnitOfAccountAsset,
    int Period,
    ValuationMethod Method = ValuationMethod.PostedPrice);

/// <summary>Pure valuation operations over authoritative economic positions.</summary>
public static class Valuation
{
    /// <summary>
    /// Attempts to value a resource-backed position. Missing prices remain
    /// unavailable instead of being treated as one unit of money per unit.
    /// </summary>
    public static bool TryValue(
        EconomyState state,
        EconomicPosition position,
        ValuationContext context,
        out Money value)
    {
        value = Money.Zero;
        if (position.Asset == context.UnitOfAccountAsset)
        {
            value = Money.From(position.Quantity);
            return true;
        }

        if (position.Region is not { } region ||
            context.Method != ValuationMethod.PostedPrice)
        {
            return false;
        }

        var resource = state.Resources.Values.FirstOrDefault(
            candidate => state.AssetFor(candidate.Id).Equals(position.Asset));
        if (resource is null)
            return false;

        var key = EconomyState.PriceKey(region, resource.Id);
        if (!state.PostedPrices.TryGetValue(key, out var price) ||
            price.UnitPrice.Amount <= 0m)
        {
            return false;
        }

        value = Money.From(position.Quantity * price.UnitPrice.Amount);
        return true;
    }
}
