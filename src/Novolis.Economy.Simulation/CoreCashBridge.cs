using Novolis.Economy.Accounting;
using Novolis.Economy.Core;
using Novolis.Economy.Core.Finance;

namespace Novolis.Economy.Simulation;

/// <summary>
/// Projects operational ledger cash changes into Core monetary positions.
/// The ledger remains journal evidence; Core is the monetary stock of record.
/// </summary>
public static class CoreCashBridge
{
  /// <summary>Attach a ledger's cash-change hook to one economy world.</summary>
  public static void Attach(EconomyWorld world, FirmLedger ledger)
  {
    ArgumentNullException.ThrowIfNull(world);
    ArgumentNullException.ThrowIfNull(ledger);
    ledger.CashChanging += delta =>
      ApplyDelta(world, ledger.FirmId, delta);
  }

  /// <summary>Apply one operational cash delta to Core's monetary position.</summary>
  public static void ApplyDelta(
    EconomyWorld world,
    FirmId firmId,
    Money delta)
  {
    if (delta.Amount == 0m)
      return;

    var entityId = firmId.AsCore();
    if (!world.CoreState.Entities.ContainsKey(entityId))
    {
      if (world.Registration != RegistrationMode.Implicit)
        throw new UnknownEconomicEntityException(entityId);

      world.CoreState = world.CoreState with
      {
        Entities = new Dictionary<LegalEntityId, Core.LegalEntity>(
          world.CoreState.Entities)
        {
          [entityId] = new Core.LegalEntity(
            entityId,
            Core.LegalEntityKind.Firm,
            Money.Zero)
        }
      };
    }

    world.CoreState = CashLedger.SetCash(
      world.CoreState,
      entityId,
      CashLedger.Balance(world.CoreState, entityId) + delta);
  }

  /// <summary>
  /// Reconciles ledgers created or changed outside the normal world hooks.
  /// </summary>
  public static void ReconcileAll(EconomyWorld world)
  {
    foreach (var ledger in world.Ledgers.Values)
    {
      if (!world.CoreState.Entities.ContainsKey(ledger.FirmId.AsCore()))
      {
        if (ledger.Cash.Amount == 0m)
          continue;

        ApplyDelta(world, ledger.FirmId, ledger.Cash);
        continue;
      }

      var coreCash = CashLedger.Balance(
        world.CoreState,
        ledger.FirmId.AsCore());
      var delta = ledger.Cash - coreCash;
      if (delta.Amount != 0m)
        ApplyDelta(world, ledger.FirmId, delta);
    }
  }
}
