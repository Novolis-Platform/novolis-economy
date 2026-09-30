using System.Collections.Immutable;
using Novolis.Economy;
using Novolis.Economy.Abstractions;

namespace Novolis.Economy.Accounting;

/// <summary>Open commercial invoice.</summary>
public sealed class Invoice
{
  /// <summary>Creates an invoice.</summary>
  public Invoice(
    Guid id,
    FirmId sellerFirmId,
    FirmId? buyerFirmId,
    Money amount,
    SimulationHour postedAt)
  {
    Id = id;
    SellerFirmId = sellerFirmId;
    BuyerFirmId = buyerFirmId;
    Amount = amount;
    Remaining = amount;
    PostedAt = postedAt;
  }

  /// <summary>Invoice id.</summary>
  public Guid Id { get; }

  /// <summary>Seller.</summary>
  public FirmId SellerFirmId { get; }

  /// <summary>Buyer (null = consumer cash sale already settled).</summary>
  public FirmId? BuyerFirmId { get; }

  /// <summary>Original amount.</summary>
  public Money Amount { get; }

  /// <summary>Unpaid remainder.</summary>
  public Money Remaining { get; set; }

  /// <summary>When posted.</summary>
  public SimulationHour PostedAt { get; }

  /// <summary>Whether fully paid.</summary>
  public bool IsSettled => Remaining.Amount <= 0m;
}
