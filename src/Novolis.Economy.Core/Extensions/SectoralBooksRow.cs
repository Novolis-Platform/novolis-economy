namespace Novolis.Economy.Core.Extensions;

/// <summary>Sectoral stock aggregate for one institutional kind.</summary>
public sealed record SectoralBooksRow(
  LegalEntityKind Kind,
  int EntityCount,
  Money Cash,
  Money DepositsHeld,
  Money DepositLiabilities,
  Money LoansReceivable,
  Money LoansPayable,
  Money ObligationsReceivable,
  Money ObligationsPayable,
  Money HoldingsValued,
  decimal HoldingsUnpricedQuantity,
  Money NetWorth);