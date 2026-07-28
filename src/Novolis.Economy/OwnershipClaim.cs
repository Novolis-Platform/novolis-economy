namespace Novolis.Economy;

/// <summary>
/// Share claim on an issuing firm (fractions per issuer should sum to ~1).
/// Primitives DTO; cash posting lives in <c>Novolis.Economy.Accounting.OwnershipEngine</c>.
/// </summary>
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
