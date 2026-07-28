namespace Novolis.Economy;

/// <summary>Party kind for legal-entity metadata (keyed by <see cref="FirmId"/>).</summary>
public enum LegalEntityKind
{
  /// <summary>Commercial firm (may issue ownership shares).</summary>
  Firm = 0,

  /// <summary>Civic / treasury party (may issue shares; product copy may say Civics).</summary>
  Civic = 1,

  /// <summary>Household sector party (owns claims; does not issue shares).</summary>
  Household = 2,
}

/// <summary>
/// Legal-entity metadata for a firm id. Ships, hubs, and facilities remain assets of an entity.
/// Household spendable liquid is cohort <c>BudgetRemaining</c> (ledger cash unused for Household).
/// </summary>
public sealed class LegalEntity
{
  /// <summary>Creates entity metadata.</summary>
  public LegalEntity(
    FirmId id,
    LegalEntityKind kind = LegalEntityKind.Firm,
    string? registryId = null)
  {
    Id = id;
    Kind = kind;
    RegistryId = registryId;
  }

  /// <summary>Same id as the firm ledger key.</summary>
  public FirmId Id { get; }

  /// <summary>Firm, civic, or household.</summary>
  public LegalEntityKind Kind { get; init; }

  /// <summary>Opaque registry / jurisdiction label (not a map place).</summary>
  public string? RegistryId { get; init; }

  /// <summary>When true, cannot borrow via <c>OriginateLoan</c>.</summary>
  public bool CreditFrozen { get; set; }

  /// <summary>Whether this entity may issue ownership claims.</summary>
  public bool CanIssueShares =>
    Kind is LegalEntityKind.Firm or LegalEntityKind.Civic;
}
