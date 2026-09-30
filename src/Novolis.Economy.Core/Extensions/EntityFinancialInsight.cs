namespace Novolis.Economy.Core.Extensions;

/// <summary>Financial insight for one legal entity in context of the economy.</summary>
public sealed record EntityFinancialInsight(
  LegalEntityId Id,
  LegalEntityKind Kind,
  Money Cash,
  Money Deposits,
  Money LoansAsBorrower,
  Money LoansAsLender,
  Money UndrawnCommittedCredit,
  Money PendingObligationsDue,
  Money PendingObligationsReceivable,
  LiquidityPosition Liquidity,
  Money SimpleSolvency,
  bool IsIlliquid,
  bool IsInsolventHint);