using Novolis.Economy.Accounting;
using Novolis.Economy.Core;
using Novolis.Economy.Core.Finance;

namespace Novolis.Economy.Simulation;

/// <summary>
/// Projects operational ownership claims into Core share holdings. Core is
/// authoritative for the economic ownership representation.
/// </summary>
public static class CoreOwnershipBridge
{
  /// <summary>Rebuild Core's common share class for one issuer.</summary>
  public static void SyncIssuer(EconomyWorld world, FirmId issuer)
  {
    var issuerId = issuer.AsCore();
    var coreEntities = new Dictionary<LegalEntityId, Core.LegalEntity>(world.CoreState.Entities);
    coreEntities.TryAdd(
      issuerId,
      new Core.LegalEntity(issuerId, Core.LegalEntityKind.Firm, Money.Zero));

    foreach (var claim in world.OwnershipClaims.Where(c => c.IssuerFirmId.Equals(issuer)))
    {
      var ownerId = claim.OwnerFirmId.AsCore();
      coreEntities.TryAdd(
        ownerId,
        new Core.LegalEntity(ownerId, Core.LegalEntityKind.Firm, Money.Zero));
    }

    var claims = world.OwnershipClaims
      .Where(c => c.IssuerFirmId.Equals(issuer))
      .ToList();
    var held = claims.Sum(c => c.Fraction);
    var classes = new Dictionary<string, ShareClass>(world.CoreState.ShareClasses)
    {
      [ShareMath.ClassKey(issuerId, "common")] =
        new ShareClass(issuerId, "common", 1m, 1m, Math.Max(0m, 1m - held))
    };

    var holdings = world.CoreState.ShareHoldings
      .Where(h => !h.Issuer.Equals(issuerId) ||
                  !string.Equals(h.ShareClass, "common", StringComparison.Ordinal))
      .ToList();
    holdings.AddRange(
      claims.Select(claim => new ShareHolding(
        claim.OwnerFirmId.AsCore(),
        issuerId,
        "common",
        claim.Fraction)));

    world.CoreState = world.CoreState with
    {
      Entities = coreEntities,
      ShareClasses = classes,
      ShareHoldings = holdings
    };
  }
}
