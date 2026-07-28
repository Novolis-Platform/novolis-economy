using Novolis.Economy;

namespace Novolis.Economy.Production;

/// <summary>Inventory keyed by firm, location, and product.</summary>
public readonly record struct InventoryKey(
  FirmId FirmId,
  InventoryLocationId LocationId,
  ProductId ProductId);

/// <summary>FIFO lot inventory store.</summary>
public sealed class InventoryStore
{
  private readonly Dictionary<InventoryKey, List<ProductBatch>> _lots = new();

  /// <summary>All keys currently holding stock.</summary>
  public IEnumerable<InventoryKey> Keys => _lots.Keys;

  /// <summary>Total quantity for a key.</summary>
  public Quantity GetQuantity(InventoryKey key)
  {
    if (!_lots.TryGetValue(key, out var lots))
    {
      return Quantity.Zero;
    }

    var sum = 0m;
    foreach (var lot in lots)
    {
      sum += lot.Quantity.Value;
    }

    return Quantity.From(sum);
  }

  /// <summary>Lots for a key (FIFO order).</summary>
  public IReadOnlyList<ProductBatch> GetLots(InventoryKey key) =>
    _lots.TryGetValue(key, out var lots) ? lots : Array.Empty<ProductBatch>();

  /// <summary>Adds a lot (merges into list).</summary>
  public void Add(InventoryKey key, ProductBatch batch)
  {
    if (batch.Quantity.Value <= 0m)
    {
      return;
    }

    if (!_lots.TryGetValue(key, out var lots))
    {
      lots = [];
      _lots[key] = lots;
    }

    lots.Add(batch);
  }

  /// <summary>Removes quantity FIFO; returns removed lots (possibly partial last lot) and total cost.</summary>
  public bool TryTake(
    InventoryKey key,
    Quantity quantity,
    out List<ProductBatch> taken,
    out Money totalCost)
  {
    taken = [];
    totalCost = Money.Zero;
    if (quantity.Value <= 0m)
    {
      return true;
    }

    if (!_lots.TryGetValue(key, out var lots) || GetQuantity(key) < quantity)
    {
      return false;
    }

    var remaining = quantity.Value;
    var cost = 0m;
    while (remaining > 0m && lots.Count > 0)
    {
      var head = lots[0];
      if (head.Quantity.Value <= remaining)
      {
        taken.Add(head);
        cost += head.UnitCost.Amount * head.Quantity.Value;
        remaining -= head.Quantity.Value;
        lots.RemoveAt(0);
      }
      else
      {
        taken.Add(head with { Quantity = Quantity.From(remaining) });
        cost += head.UnitCost.Amount * remaining;
        lots[0] = head with { Quantity = Quantity.From(head.Quantity.Value - remaining) };
        remaining = 0m;
      }
    }

    totalCost = Money.From(cost);
    if (lots.Count == 0)
    {
      _lots.Remove(key);
    }

    return true;
  }

  /// <summary>Fingerprint for hashing.</summary>
  public ulong Fingerprint()
  {
    const ulong offset = 14695981039346656037UL;
    const ulong prime = 1099511628211UL;
    var hash = offset;
    foreach (var key in _lots.Keys.OrderBy(k => k.FirmId.Value).ThenBy(k => k.LocationId.Value).ThenBy(k => k.ProductId.Value))
    {
      hash = (hash ^ (ulong)key.FirmId.Value.GetHashCode()) * prime;
      hash = (hash ^ (ulong)key.LocationId.Value.GetHashCode()) * prime;
      hash = (hash ^ (ulong)key.ProductId.Value.GetHashCode()) * prime;
      foreach (var lot in _lots[key])
      {
        foreach (var b in decimal.GetBits(lot.Quantity.Value))
        {
          hash = (hash ^ (ulong)(uint)b) * prime;
        }

        foreach (var b in decimal.GetBits(lot.UnitCost.Amount))
        {
          hash = (hash ^ (ulong)(uint)b) * prime;
        }
      }
    }

    return hash;
  }
}
