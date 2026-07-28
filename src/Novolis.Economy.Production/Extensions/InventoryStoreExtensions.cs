namespace Novolis.Economy.Production.Extensions;

/// <summary>Read-only inventory store insights.</summary>
public static class InventoryStoreExtensions
{
    public static InventorySnapshot Snapshot(this InventoryStore store)
    {
        var byFirm = new Dictionary<FirmId, decimal>();
        var byProduct = new Dictionary<ProductId, decimal>();
        var totalQty = 0m;
        var totalCost = 0m;
        var slots = 0;

        foreach (var key in store.Keys)
        {
            slots++;
            var lots = store.GetLots(key);
            var qty = 0m;
            var cost = 0m;
            foreach (var lot in lots)
            {
                qty += lot.Quantity.Value;
                cost += lot.UnitCost.Amount * lot.Quantity.Value;
            }

            totalQty += qty;
            totalCost += cost;
            byFirm[key.FirmId] = byFirm.GetValueOrDefault(key.FirmId) + qty;
            byProduct[key.ProductId] = byProduct.GetValueOrDefault(key.ProductId) + qty;
        }

        return new InventorySnapshot(
            SlotCount: slots,
            TotalQuantity: totalQty,
            TotalBookCost: Money.From(totalCost),
            QuantityByFirm: byFirm,
            QuantityByProduct: byProduct);
    }
}
