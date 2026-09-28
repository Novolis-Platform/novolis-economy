namespace Novolis.Economy.Primitives;

/// <summary>A non-negative quantity denominated in one economic asset.</summary>
public readonly record struct AssetAmount
{
    public AssetAmount(EconomicAssetId asset, decimal quantity)
    {
        if (quantity < 0m)
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Asset quantities cannot be negative.");

        Asset = asset;
        Quantity = quantity;
    }

    public EconomicAssetId Asset { get; }
    public decimal Quantity { get; }
}
