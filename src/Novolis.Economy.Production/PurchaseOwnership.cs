using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Pay cash for an ownership fraction (households debit BudgetRemaining).</summary>
public sealed record PurchaseOwnership(
  FirmId IssuerFirmId,
  FirmId BuyerFirmId,
  decimal Fraction,
  Money Price) : IEconomyCommand;
