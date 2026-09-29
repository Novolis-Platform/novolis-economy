using System.Text.Json.Serialization;

namespace Novolis.Economy.Primitives;

/// <summary>
/// A quantity of one economic asset owned or controlled by one economic entity.
/// The quantity is not a monetary valuation.
/// </summary>
public readonly record struct EconomicPosition
{
    [JsonConstructor]
    public EconomicPosition(
        EconomicEntityId owner,
        EconomicAssetId asset,
        decimal quantity,
        RegionId? region = null)
    {
        if (quantity < 0m)
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Position quantities cannot be negative.");

        Owner = owner;
        Asset = asset;
        Quantity = quantity;
        Region = region;
    }

    public EconomicEntityId Owner { get; }
    public EconomicAssetId Asset { get; }
    public decimal Quantity { get; }
    public RegionId? Region { get; }
}
