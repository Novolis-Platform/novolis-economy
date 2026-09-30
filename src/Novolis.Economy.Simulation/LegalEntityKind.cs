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

  /// <summary>Explicit non-domestic counterparty for open-sector settlement.</summary>
  ExternalSector = 3,

  /// <summary>Deposit-taking lender used by endogenous credit profiles.</summary>
  Bank = 4,
}
