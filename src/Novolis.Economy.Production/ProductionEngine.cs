using Novolis.Economy;

namespace Novolis.Economy.Production;

/// <summary>Converts planned production into output batches using recipe inputs.</summary>
public static class ProductionEngine
{
  /// <summary>
  /// Attempts to produce up to <paramref name="plannedUnits"/> of <paramref name="product"/>,
  /// limited by inputs, capacity, labor, and productivity.
  /// </summary>
  public static Quantity TryProduce(
    ProductDefinition product,
    InventoryStore inventory,
    FirmId firmId,
    InventoryLocationId storageLocation,
    Quantity plannedUnits,
    Quantity manufacturingCapacity,
    decimal laborHours,
    decimal laborHoursPerUnit,
    decimal productivity,
    SimulationDate producedAt,
    out Money unitCost)
  {
    unitCost = Money.Zero;
    if (plannedUnits.Value <= 0m)
    {
      return Quantity.Zero;
    }

    var capacityLimit = manufacturingCapacity.Value;
    var laborLimit = laborHoursPerUnit <= 0m
      ? plannedUnits.Value
      : laborHours / laborHoursPerUnit;
    var maxByPlan = Math.Min(plannedUnits.Value, capacityLimit);
    maxByPlan = Math.Min(maxByPlan, laborLimit) * Math.Max(0.01m, productivity);
    maxByPlan = Math.Floor(maxByPlan * 10000m) / 10000m;
    if (maxByPlan <= 0m)
    {
      return Quantity.Zero;
    }

    // Limit by available inputs
    var producible = maxByPlan;
    foreach (var input in product.Inputs)
    {
      var available = inventory.GetQuantity(new InventoryKey(firmId, storageLocation, input.ProductId));
      var neededPer = input.QuantityPerOutput.Value;
      if (neededPer <= 0m)
      {
        continue;
      }

      producible = Math.Min(producible, available.Value / neededPer);
    }

    producible = Math.Floor(producible * 10000m) / 10000m;
    if (producible <= 0m)
    {
      return Quantity.Zero;
    }

    var outputQty = Quantity.From(producible);
    var totalInputCost = 0m;
    foreach (var input in product.Inputs)
    {
      var need = Quantity.From(input.QuantityPerOutput.Value * producible);
      var key = new InventoryKey(firmId, storageLocation, input.ProductId);
      if (!inventory.TryTake(key, need, out _, out var cost))
      {
        return Quantity.Zero;
      }

      totalInputCost += cost.Amount;
    }

    unitCost = Money.From(totalInputCost / producible);
    inventory.Add(
      new InventoryKey(firmId, storageLocation, product.Id),
      new ProductBatch(
        product.Id,
        outputQty,
        new ProductQuality(100m),
        unitCost,
        producedAt,
        BrandId: null));
    return outputQty;
  }

  /// <summary>Removes expired lots when shelf life is configured.</summary>
  public static List<(InventoryKey Key, Quantity Qty, Money Cost)> ApplySpoilage(
    InventoryStore inventory,
    IReadOnlyDictionary<ProductId, ProductDefinition> products,
    SimulationHour now)
  {
    var spoiled = new List<(InventoryKey, Quantity, Money)>();
    foreach (var key in inventory.Keys.ToList())
    {
      if (!products.TryGetValue(key.ProductId, out var def) || def.ShelfLife is null)
      {
        continue;
      }

      var life = def.ShelfLife.Value.Hours;
      var lots = inventory.GetLots(key).ToList();
      inventory.TryTake(key, inventory.GetQuantity(key), out _, out _); // clear
      foreach (var lot in lots)
      {
        var age = now.HourIndex - (long)lot.ProducedAt.DayIndex * SimulationHour.HoursPerDay;
        if (age >= life)
        {
          spoiled.Add((key, lot.Quantity, Money.From(lot.UnitCost.Amount * lot.Quantity.Value)));
        }
        else
        {
          inventory.Add(key, lot);
        }
      }
    }

    return spoiled;
  }
}
