using Novolis.Economy;

namespace Novolis.Economy.Markets;

/// <summary>Open hub spot order (mutable remaining quantity).</summary>
public sealed class HubOrder
{
  /// <summary>Creates a hub order.</summary>
  public HubOrder(
    Guid id,
    FirmId firmId,
    InventoryLocationId locationId,
    ProductId productId,
    HubOrderSide side,
    Quantity quantity,
    Money limitPrice,
    SimulationHour postedAt)
  {
    Id = id;
    FirmId = firmId;
    LocationId = locationId;
    ProductId = productId;
    Side = side;
    Remaining = quantity;
    LimitPrice = limitPrice;
    PostedAt = postedAt;
  }

  /// <summary>Order id.</summary>
  public Guid Id { get; }

  /// <summary>Owning firm.</summary>
  public FirmId FirmId { get; }

  /// <summary>Hub inventory location.</summary>
  public InventoryLocationId LocationId { get; }

  /// <summary>Product.</summary>
  public ProductId ProductId { get; }

  /// <summary>Buy or sell.</summary>
  public HubOrderSide Side { get; }

  /// <summary>Unfilled quantity.</summary>
  public Quantity Remaining { get; set; }

  /// <summary>Limit price per unit.</summary>
  public Money LimitPrice { get; }

  /// <summary>Posting hour (FIFO).</summary>
  public SimulationHour PostedAt { get; }

  /// <summary>True when nothing remains.</summary>
  public bool IsFilled => Remaining.Value <= 0m;
}
