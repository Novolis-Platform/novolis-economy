using Novolis.Economy;

namespace Novolis.Economy.Accounting;

/// <summary>Share claim on an issuing firm (fractions per issuer should sum to ~1).</summary>
public sealed class OwnershipClaim
{
  /// <summary>Creates a claim.</summary>
  public OwnershipClaim(FirmId issuerFirmId, FirmId ownerFirmId, decimal fraction)
  {
    IssuerFirmId = issuerFirmId;
    OwnerFirmId = ownerFirmId;
    Fraction = fraction;
  }

  /// <summary>Firm that issued the claim.</summary>
  public FirmId IssuerFirmId { get; }

  /// <summary>Owner of the fraction.</summary>
  public FirmId OwnerFirmId { get; set; }

  /// <summary>Ownership fraction in [0, 1].</summary>
  public decimal Fraction { get; set; }
}

/// <summary>Pure ownership + dividend helpers.</summary>
public static class OwnershipEngine
{
  /// <summary>Upserts an absolute fraction for one owner; fails if totals would exceed 1.</summary>
  public static bool TryAssign(
    IList<OwnershipClaim> claims,
    FirmId issuer,
    FirmId owner,
    decimal fraction,
    Func<FirmId, bool> canIssueShares)
  {
    if (fraction < 0m || fraction > 1m + 0.0000001m || !canIssueShares(issuer))
    {
      return false;
    }

    var existing = claims.Where(c => c.IssuerFirmId.Equals(issuer)).ToList();
    var others = existing.Where(c => !c.OwnerFirmId.Equals(owner)).Sum(c => c.Fraction);
    if (others + fraction > 1m + 0.0000001m)
    {
      return false;
    }

    var mine = existing.FirstOrDefault(c => c.OwnerFirmId.Equals(owner));
    if (fraction <= 0.0000001m)
    {
      if (mine is not null)
      {
        claims.Remove(mine);
      }

      return true;
    }

    if (mine is null)
    {
      claims.Add(new OwnershipClaim(issuer, owner, fraction));
    }
    else
    {
      mine.Fraction = fraction;
    }

    return true;
  }

  /// <summary>Moves <paramref name="fraction"/> from one owner to another on the same issuer.</summary>
  public static bool TryTransfer(
    IList<OwnershipClaim> claims,
    FirmId issuer,
    FirmId fromOwner,
    FirmId toOwner,
    decimal fraction,
    Func<FirmId, bool> canIssueShares)
  {
    if (fraction <= 0m || fromOwner.Equals(toOwner) || !canIssueShares(issuer))
    {
      return false;
    }

    var from = claims.FirstOrDefault(c =>
      c.IssuerFirmId.Equals(issuer) && c.OwnerFirmId.Equals(fromOwner));
    if (from is null || from.Fraction + 0.0000001m < fraction)
    {
      return false;
    }

    from.Fraction -= fraction;
    if (from.Fraction <= 0.0000001m)
    {
      claims.Remove(from);
    }

    var to = claims.FirstOrDefault(c =>
      c.IssuerFirmId.Equals(issuer) && c.OwnerFirmId.Equals(toOwner));
    if (to is null)
    {
      claims.Add(new OwnershipClaim(issuer, toOwner, fraction));
    }
    else
    {
      to.Fraction += fraction;
    }

    return true;
  }

  /// <summary>
  /// Moves all claims issued by <paramref name="issuer"/> to <paramref name="newOwner"/> (merge).
  /// </summary>
  public static void TransferAllIssuerClaimsTo(
    IList<OwnershipClaim> claims,
    FirmId issuer,
    FirmId newOwner)
  {
    var issued = claims.Where(c => c.IssuerFirmId.Equals(issuer)).ToList();
    if (issued.Count == 0)
    {
      return;
    }

    var total = issued.Sum(c => c.Fraction);
    foreach (var c in issued)
    {
      claims.Remove(c);
    }

    if (total <= 0.0000001m)
    {
      return;
    }

    var existing = claims.FirstOrDefault(c =>
      c.IssuerFirmId.Equals(issuer) && c.OwnerFirmId.Equals(newOwner));
    if (existing is null)
    {
      claims.Add(new OwnershipClaim(issuer, newOwner, total));
    }
    else
    {
      existing.Fraction = Math.Min(1m, existing.Fraction + total);
    }
  }

  /// <summary>
  /// Pays dividend from issuer cash to owners pro-rata. Conserves liquid.
  /// Short-pays in owner-id order when cash is insufficient for the full total.
  /// </summary>
  public static IReadOnlyList<(FirmId Owner, Money Amount)> TryDeclareDividend(
    IList<OwnershipClaim> claims,
    IDictionary<FirmId, FirmLedger> ledgers,
    FirmId issuer,
    Money total,
    SimulationDate date)
  {
    var paid = new List<(FirmId, Money)>();
    if (total.Amount <= 0m
        || !ledgers.TryGetValue(issuer, out var issuerLedger)
        || issuerLedger.Cash.Amount <= 0m)
    {
      return paid;
    }

    var owners = claims
      .Where(c => c.IssuerFirmId.Equals(issuer) && c.Fraction > 0m)
      .OrderBy(c => c.OwnerFirmId.Value)
      .ToList();
    if (owners.Count == 0)
    {
      return paid;
    }

    var pool = Math.Min(total.Amount, issuerLedger.Cash.Amount);
    var fractionSum = owners.Sum(c => c.Fraction);
    if (fractionSum <= 0m)
    {
      return paid;
    }

    foreach (var claim in owners)
    {
      if (pool <= 0.0000001m)
      {
        break;
      }

      var share = pool * (claim.Fraction / fractionSum);
      // Recompute remaining pool after rounding by using min remaining cash.
      share = Math.Min(share, issuerLedger.Cash.Amount);
      if (share <= 0.0000001m || !ledgers.TryGetValue(claim.OwnerFirmId, out var ownerLedger))
      {
        continue;
      }

      var money = Money.From(Math.Round(share, 6, MidpointRounding.AwayFromZero));
      if (money.Amount <= 0m || issuerLedger.Cash.Amount + 0.0000001m < money.Amount)
      {
        continue;
      }

      PostDividend(issuerLedger, ownerLedger, money, date);
      paid.Add((claim.OwnerFirmId, money));
      pool -= money.Amount;
    }

    return paid;
  }

  /// <summary>Issuer equity↓/cash↓; owner cash↑/equity↑.</summary>
  public static void PostDividend(
    FirmLedger issuer,
    FirmLedger owner,
    Money amount,
    SimulationDate date)
  {
    issuer.Post(AccountRole.Equity, AccountRole.Cash, amount, date, "Dividend paid");
    owner.Post(AccountRole.Cash, AccountRole.Equity, amount, date, "Dividend received");
  }

  /// <summary>Spends cash on capacity investment (burns firm liquid into equity reduction).</summary>
  public static bool TryPostCapacityInvestment(FirmLedger owner, Money cost, SimulationDate date)
  {
    if (cost.Amount <= 0m)
    {
      return true;
    }

    if (owner.Cash.Amount + 0.0000001m < cost.Amount)
    {
      return false;
    }

    owner.Post(AccountRole.Equity, AccountRole.Cash, cost, date, "Capacity investment");
    return true;
  }
}
