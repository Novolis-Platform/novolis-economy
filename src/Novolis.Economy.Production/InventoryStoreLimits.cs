using Novolis.Economy;

namespace Novolis.Economy.Production;

/// <summary>
/// Soft/hard warehouse caps per (location, product). Soft guides agent surplus policy;
/// hard truncates <see cref="InventoryStore.Add"/> (location-total across firms).
/// </summary>
public sealed class InventoryStoreLimits
{
  private readonly Dictionary<(Guid Location, Guid Product), (decimal Soft, decimal Hard)> _caps = new();

  /// <summary>Sets soft (surplus threshold) and hard (physical) caps. Hard must be ≥ soft.</summary>
  public void Set(InventoryLocationId location, ProductId product, decimal softCap, decimal hardCap)
  {
    ArgumentOutOfRangeException.ThrowIfNegative(softCap);
    ArgumentOutOfRangeException.ThrowIfNegative(hardCap);
    if (hardCap < softCap)
    {
      throw new ArgumentException("Hard cap must be >= soft cap.", nameof(hardCap));
    }

    _caps[(location.Value, product.Value)] = (softCap, hardCap);
  }

  /// <summary>Whether a hard cap is configured for this slot.</summary>
  public bool TryGetHard(InventoryLocationId location, ProductId product, out decimal hardCap)
  {
    if (_caps.TryGetValue((location.Value, product.Value), out var caps))
    {
      hardCap = caps.Hard;
      return true;
    }

    hardCap = 0m;
    return false;
  }

  /// <summary>Soft surplus threshold, or null if unset.</summary>
  public decimal? SoftCap(InventoryLocationId location, ProductId product) =>
    _caps.TryGetValue((location.Value, product.Value), out var caps) ? caps.Soft : null;

  /// <summary>Hard physical cap, or null if unset (unlimited).</summary>
  public decimal? HardCap(InventoryLocationId location, ProductId product) =>
    _caps.TryGetValue((location.Value, product.Value), out var caps) ? caps.Hard : null;

  /// <summary>Quantity of <paramref name="product"/> at <paramref name="location"/> across all firms.</summary>
  public static decimal OnHand(InventoryStore store, InventoryLocationId location, ProductId product)
  {
    var sum = 0m;
    foreach (var key in store.Keys)
    {
      if (key.LocationId.Equals(location) && key.ProductId.Equals(product))
      {
        sum += store.GetQuantity(key).Value;
      }
    }

    return sum;
  }

  /// <summary>Room under hard cap (0 if full or unlimited when unset → large).</summary>
  public decimal Room(InventoryStore store, InventoryLocationId location, ProductId product)
  {
    if (!TryGetHard(location, product, out var hard))
    {
      return decimal.MaxValue / 4m;
    }

    return Math.Max(0m, hard - OnHand(store, location, product));
  }

  /// <summary>True when on-hand strictly exceeds soft cap (exportable surplus).</summary>
  public bool IsLargeSurplus(InventoryStore store, InventoryLocationId location, ProductId product)
  {
    var soft = SoftCap(location, product);
    if (soft is null)
    {
      return false;
    }

    return OnHand(store, location, product) > soft.Value;
  }

  /// <summary>Units above soft cap (0 if not in surplus).</summary>
  public decimal Surplus(InventoryStore store, InventoryLocationId location, ProductId product)
  {
    var soft = SoftCap(location, product);
    if (soft is null)
    {
      return 0m;
    }

    return Math.Max(0m, OnHand(store, location, product) - soft.Value);
  }
}
