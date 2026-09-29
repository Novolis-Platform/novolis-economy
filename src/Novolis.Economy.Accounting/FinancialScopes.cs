using Novolis.Economy.Core;

namespace Novolis.Economy.Accounting;

/// <summary>Selection of economic owners whose financials should be projected.</summary>
public abstract record FinancialScope
{
  /// <summary>One legal entity.</summary>
  public sealed record Entity(LegalEntityId Id) : FinancialScope;

  /// <summary>All linked economic entities for one household cohort.</summary>
  public sealed record Cohort(CohortId Id) : FinancialScope;

  /// <summary>Entities with positions or activities in one region.</summary>
  public sealed record Region(RegionId Id) : FinancialScope;

  /// <summary>All legal entities of one institutional kind.</summary>
  public sealed record EntityKind(LegalEntityKind Kind) : FinancialScope;

  /// <summary>Explicit group of legal entities.</summary>
  public sealed record Group(IReadOnlySet<LegalEntityId> Ids) : FinancialScope;

  /// <summary>Named sector backed by an explicit set of legal entities.</summary>
  public sealed record Sector(
    string Name,
    IReadOnlySet<LegalEntityId> Ids) : FinancialScope;

  /// <summary>All entities belonging to the explicit external sector.</summary>
  public sealed record ExternalSector : FinancialScope;
}
