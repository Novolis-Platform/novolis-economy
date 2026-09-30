namespace Novolis.Economy.Core.Extensions;

/// <summary>Full projected accounts snapshot for reporting (Core-only).</summary>
public sealed record ProjectedAccountsSnapshot(
  IReadOnlyList<SectoralBooksRow> Sectors,
  IReadOnlyList<ProjectedBalanceSheet> Entities,
  ProjectedPeriodIncome LastPeriod,
  Money AggregateNetWorth,
  decimal AggregateHoldingsUnpricedQuantity);