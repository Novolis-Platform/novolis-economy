using Novolis.Economy.Core;
using Novolis.Economy.Core.Finance;

namespace Novolis.Economy.Simulation;

/// <summary>Registers Core monetary positions for operational firm identities.</summary>
public static class CoreMonetaryBridge
{
  /// <summary>Ensure a Core entity and seed its authoritative monetary position.</summary>
  public static void EnsureEntity(
    EconomyWorld world,
    FirmId firmId,
    Core.LegalEntityKind kind,
    Money openingCash = default)
  {
    var id = firmId.AsCore();
    if (!world.CoreState.Entities.TryGetValue(id, out var existing))
    {
      var entities = new Dictionary<LegalEntityId, Core.LegalEntity>(world.CoreState.Entities)
      {
        [id] = new Core.LegalEntity(id, kind, Money.Zero)
      };
      world.CoreState = world.CoreState with { Entities = entities };
    }
    else if (existing.Kind != kind)
    {
      var entities = new Dictionary<LegalEntityId, Core.LegalEntity>(world.CoreState.Entities)
      {
        [id] = existing with { Kind = kind }
      };
      world.CoreState = world.CoreState with { Entities = entities };
    }

    if (openingCash.Amount != 0m)
      world.CoreState = CashLedger.SetCash(world.CoreState, id, openingCash);
  }
}
