namespace Novolis.Economy.Core.Extensions;

/// <summary>Obligation book by status and kind.</summary>
public sealed record ObligationBookInsight(
  int PendingCount,
  int DelinquentCount,
  int DefaultedCount,
  int PaidCount,
  Money PendingSum,
  Money DelinquentSum,
  Money DueNow,
  IReadOnlyDictionary<ObligationKind, Money> PendingSumByKind);