namespace Novolis.Economy.Models.SmallOpenRegionalTrade;

/// <summary>How the household food market forms a transaction price.</summary>
public enum FoodMarketMechanism
{
    /// <summary>Use the declared posted price and ration scarce supply.</summary>
    PostedPriceRationing = 0,

    /// <summary>Adjust the posted price from declared demand pressure.</summary>
    SupplyDemandPriceDiscovery = 1
}
