using Novolis.Economy.Core;
using Novolis.Economy.Core.Holdings;
using Novolis.Economy.Core.Transactions;
using Novolis.Economy.Production;

namespace Novolis.Economy.Simulation;

/// <summary>
/// Reconciles lot-level operational inventory with Core's authoritative
/// owner × asset × region positions.
/// </summary>
public static class CoreInventoryBridge
{
  /// <summary>Returns the Core region associated with an inventory location.</summary>
  public static RegionId? RegionFor(
    EconomyWorld world,
    InventoryLocationId locationId)
  {
    return world.InventoryLocationRegions.TryGetValue(locationId, out var region)
      && world.CoreState.Regions.ContainsKey(region)
        ? region
        : null;
  }

  /// <summary>Adds an operational lot and records its aggregate Core position.</summary>
  public static Quantity Add(
    EconomyWorld world,
    InventoryKey key,
    ProductBatch batch,
    bool bypassLimits = false)
  {
    var accepted = world.Inventory.Add(key, batch, bypassLimits);
    if (accepted.Value > 0m)
    {
      ApplyDelta(world, key, accepted.Value, "inventory-add");
    }

    return accepted;
  }

  /// <summary>
  /// Removes operational lots after checking and debiting the Core position.
  /// </summary>
  public static bool TryTake(
    EconomyWorld world,
    InventoryKey key,
    Quantity quantity,
    out List<ProductBatch> taken,
    out Money totalCost)
  {
    taken = [];
    totalCost = Money.Zero;
    if (quantity.Value <= 0m)
      return true;

    var asset = world.CoreState.AssetFor(key.ProductId.AsCore());
    var owner = EconomicIdentity.For(key.FirmId.AsCore());
    var region = RegionFor(world, key.LocationId);
    var available = PositionLedger.GetQuantity(
      world.CoreState,
      owner,
      region,
      asset);
    if (available + 1e-12m < quantity.Value)
    {
      ReconcileAll(world);
      available = PositionLedger.GetQuantity(
        world.CoreState,
        owner,
        region,
        asset);
    }
    if (available + 1e-12m < quantity.Value)
      return false;

    if (!world.Inventory.TryTake(key, quantity, out taken, out totalCost))
      return false;

    ApplyDelta(world, key, -quantity.Value, "inventory-take");
    return true;
  }

  /// <summary>
  /// Reconciles all currently visible operational lots with Core positions.
  /// This is the migration seam for callers that still seed or manipulate
  /// <see cref="InventoryStore"/> directly.
  /// </summary>
  public static void ReconcileAll(EconomyWorld world)
  {
    var snapshot = Snapshot(world);
    ReconcileSnapshot(world, snapshot, snapshot.Keys);
  }

  /// <summary>Captures aggregate operational quantities by inventory slot.</summary>
  public static IReadOnlyDictionary<InventoryKey, decimal> Snapshot(
    EconomyWorld world)
  {
    return world.Inventory.Keys
      .ToDictionary(key => key, key => world.Inventory.GetQuantity(key).Value);
  }

  /// <summary>
  /// Applies only the changes between two operational inventory snapshots.
  /// </summary>
  public static void ReconcileChanges(
    EconomyWorld world,
    IReadOnlyDictionary<InventoryKey, decimal> before)
  {
    var after = Snapshot(world);
    ReconcileSnapshot(
      world,
      after,
      before.Keys.Concat(after.Keys).Distinct());
  }

  private static void ReconcileSnapshot(
    EconomyWorld world,
    IReadOnlyDictionary<InventoryKey, decimal> snapshot,
    IEnumerable<InventoryKey> representativeKeys)
  {
    var desired = snapshot
      .GroupBy(item => Slot(world, item.Key))
      .ToDictionary(group => group.Key, group => group.Sum(item => item.Value));
    var current = PositionLedger.Snapshot(world.CoreState)
      .Values
      .Where(position => position.Asset != world.CoreState.MonetaryAssetId)
      .GroupBy(position => (
        Owner: position.Owner,
        Region: position.Region,
        Asset: position.Asset))
      .ToDictionary(group => group.Key, group => group.Sum(position => position.Quantity));
    var representatives = representativeKeys
      .GroupBy(key => Slot(world, key))
      .ToDictionary(group => group.Key, group => group.First());

    foreach (var slot in desired.Keys.Concat(representatives.Keys).Distinct())
    {
      var delta = desired.GetValueOrDefault(slot) - current.GetValueOrDefault(slot);
      if (Math.Abs(delta) > 1e-12m &&
          representatives.TryGetValue(slot, out var representative))
      {
        ApplyDelta(world, representative, delta, "inventory-reconcile");
      }
    }
  }

  private static (
    EconomicEntityId Owner,
    RegionId? Region,
    EconomicAssetId Asset) Slot(
    EconomyWorld world,
    InventoryKey key)
  {
    return (
      EconomicIdentity.For(key.FirmId.AsCore()),
      RegionFor(world, key.LocationId),
      world.CoreState.AssetFor(key.ProductId.AsCore()));
  }

  private static void ApplyDelta(
    EconomyWorld world,
    InventoryKey key,
    decimal delta,
    string reason)
  {
    if (delta == 0m)
      return;

    var asset = world.CoreState.AssetFor(key.ProductId.AsCore());
    var owner = EconomicIdentity.For(key.FirmId.AsCore());
    var region = RegionFor(world, key.LocationId);
    world.CoreState.ValidatePositionChange(owner, asset, region);
    world.CoreState = EconomicTransactionEngine.Apply(
      world.CoreState,
      EconomicTransaction.Create(
        world.CoreState,
        [
          new PositionChange(owner, asset, delta, region)
        ],
        reason));
  }
}
