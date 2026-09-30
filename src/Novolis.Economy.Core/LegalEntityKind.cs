namespace Novolis.Economy.Core;

/// <summary>Institutional role of a legal entity (SPEC §2).</summary>
public enum LegalEntityKind
{
  /// <summary>Unownable private beneficiary; supplies labor and consumes.</summary>
  Household = 0,

  /// <summary>Ownable commercial operator; may issue shares and run activities.</summary>
  Firm,

  /// <summary>Extends credit from owned or borrowed funds (not a bank).</summary>
  Lender,

  /// <summary>Accepts deposits; may create deposit liabilities when lending.</summary>
  Bank,

  /// <summary>Receives premiums and accepts specified risks.</summary>
  Insurer,

  /// <summary>Policy authority and fiscal actor.</summary>
  State,

  /// <summary>Counterparty for explicitly settled imports and exports.</summary>
  ExternalSector
}