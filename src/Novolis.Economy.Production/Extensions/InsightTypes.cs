namespace Novolis.Economy.Production.Extensions;

/// <summary>Inventory store snapshot.</summary>
public sealed record InventorySnapshot(
    int SlotCount,
    decimal TotalQuantity,
    Money TotalBookCost,
    IReadOnlyDictionary<FirmId, decimal> QuantityByFirm,
    IReadOnlyDictionary<ProductId, decimal> QuantityByProduct);
