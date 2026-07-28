using System.Collections.Immutable;
using Novolis.Economy;
using Novolis.Economy.Accounting;
using Novolis.Economy.Production;

namespace Novolis.Economy.Simulation;

/// <summary>Default absorb: freeze credit, rebind facilities, transfer issuer ownership.</summary>
public static class DefaultConsequenceEngine
{
  /// <summary>Applies absorb side-effects for a defaulted loan.</summary>
  public static void ApplyAbsorb(
    EconomyWorld world,
    FirmId lenderFirmId,
    FirmId borrowerFirmId,
    SimulationHour hour,
    Action<IEconomyEvent> appendEvent)
  {
    if (!world.Entities.TryGetValue(borrowerFirmId, out var borrowerEntity))
    {
      borrowerEntity = new LegalEntity(borrowerFirmId);
      world.Entities[borrowerFirmId] = borrowerEntity;
    }

    if (!borrowerEntity.CreditFrozen)
    {
      borrowerEntity.CreditFrozen = true;
      appendEvent(new CreditFrozenSet(hour, borrowerFirmId));
    }

    var facilities = world.Facilities.Values
      .Where(f => f.FirmId.Equals(borrowerFirmId))
      .ToList();
    foreach (var facility in facilities)
    {
      world.Facilities[facility.Id] = new FacilityBinding(
        facility.Id,
        lenderFirmId,
        facility.StorageLocation,
        facility.RetailLocation,
        facility.Layout,
        facility.Area);
      appendEvent(new FacilityAbsorbed(hour, facility.Id, borrowerFirmId, lenderFirmId));
    }

    var before = world.OwnershipClaims
      .Where(c => c.IssuerFirmId.Equals(borrowerFirmId))
      .Select(c => (c.OwnerFirmId, c.Fraction))
      .ToList();
    OwnershipEngine.TransferAllIssuerClaimsTo(
      world.OwnershipClaims, borrowerFirmId, lenderFirmId);
    foreach (var claim in world.OwnershipClaims.Where(c => c.IssuerFirmId.Equals(borrowerFirmId)))
    {
      appendEvent(new OwnershipChanged(hour, borrowerFirmId, claim.OwnerFirmId, claim.Fraction));
    }

    // If there were claims that vanished into lender, still emit for transparency when empty→full.
    if (before.Count > 0
        && !world.OwnershipClaims.Any(c => c.IssuerFirmId.Equals(borrowerFirmId)))
    {
      foreach (var (owner, frac) in before)
      {
        appendEvent(new OwnershipChanged(hour, borrowerFirmId, owner, 0m));
      }
    }
  }

  /// <summary>Scales manufacturing/assembly unit capacities; returns new binding or null.</summary>
  public static FacilityBinding? TryUpgradeFacility(
    EconomyWorld world,
    UpgradeFacility cmd,
    SimulationHour hour,
    out string? failReason)
  {
    failReason = null;
    if (cmd.CapacityFactor <= 1m)
    {
      failReason = "factor";
      return null;
    }

    if (!world.Facilities.TryGetValue(cmd.FacilityId, out var facility))
    {
      failReason = "facility";
      return null;
    }

    if (!world.Ledgers.TryGetValue(facility.FirmId, out var ledger))
    {
      failReason = "ledger";
      return null;
    }

    if (!OwnershipEngine.TryPostCapacityInvestment(ledger, cmd.Cost, hour.Date))
    {
      failReason = "cash";
      return null;
    }

    var units = facility.Layout.Units.ToImmutableDictionary(
      kv => kv.Key,
      kv =>
      {
        var u = kv.Value;
        if (u.Kind is not (OperatingUnitKind.Manufacturing or OperatingUnitKind.Assembly))
        {
          return u;
        }

        return u with
        {
          Capacity = Quantity.From(
            Math.Round(u.Capacity.Value * cmd.CapacityFactor, 6, MidpointRounding.AwayFromZero)),
        };
      });

    var upgraded = new FacilityBinding(
      facility.Id,
      facility.FirmId,
      facility.StorageLocation,
      facility.RetailLocation,
      facility.Layout with { Units = units },
      facility.Area);
    world.Facilities[facility.Id] = upgraded;
    return upgraded;
  }
}
