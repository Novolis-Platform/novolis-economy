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
    EnsureEntity(world, coreEntities, issuer);

    foreach (var claim in world.OwnershipClaims.Where(c => c.IssuerFirmId.Equals(issuer)))
    {
      EnsureEntity(world, coreEntities, claim.OwnerFirmId);
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

  /// <summary>
  /// Reads the Core share book as the ownership input for operational
  /// behaviors that still accept Accounting compatibility DTOs.
  /// </summary>
  public static IReadOnlyList<OwnershipClaim> ClaimsFor(
    EconomyWorld world,
    FirmId issuer)
  {
    var issuerId = issuer.AsCore();
    var shareClass = world.CoreState.ShareClasses.Values.FirstOrDefault(
      share => share.Issuer.Equals(issuerId) &&
               string.Equals(share.Name, "common", StringComparison.Ordinal));
    if (shareClass is null || shareClass.IssuedUnits <= 0m)
      return Array.Empty<OwnershipClaim>();

    return world.CoreState.ShareHoldings
      .Where(holding => holding.Issuer.Equals(issuerId) &&
                       string.Equals(holding.ShareClass, "common", StringComparison.Ordinal) &&
                       holding.Units > 0m)
      .Select(holding => new OwnershipClaim(
        issuer,
        FirmId.From(holding.Owner.Value),
        holding.Units / shareClass.IssuedUnits))
      .ToList();
  }

  private static void EnsureEntity(
    EconomyWorld world,
    IDictionary<LegalEntityId, Core.LegalEntity> entities,
    FirmId firmId)
  {
    var id = firmId.AsCore();
    if (entities.ContainsKey(id))
      return;

    if (world.Registration != RegistrationMode.Implicit)
      throw new UnknownEconomicEntityException(id);

    var kind = world.Entities.TryGetValue(firmId, out var entity)
      ? entity.Kind switch
      {
        LegalEntityKind.Household => Core.LegalEntityKind.Household,
        LegalEntityKind.Civic => Core.LegalEntityKind.State,
        _ => Core.LegalEntityKind.Firm
      }
      : Core.LegalEntityKind.Firm;
    entities[id] = new Core.LegalEntity(id, kind, Money.Zero);
  }
}
