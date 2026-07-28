using Novolis.Economy;

namespace Novolis.Economy.Accounting;

/// <summary>Identifies a ledger account.</summary>
/// <param name="Value">Opaque account key.</param>
public readonly record struct AccountId(Guid Value)
{
  /// <summary>Creates a new account id.</summary>
  public static AccountId New() => new(Guid.NewGuid());

  /// <inheritdoc />
  public override string ToString() => Value.ToString("N");
}

/// <summary>Debit or credit side of an entry.</summary>
public enum LedgerSide
{
  /// <summary>Debit.</summary>
  Debit = 0,
  /// <summary>Credit.</summary>
  Credit = 1,
}

/// <summary>Single balanced ledger line (skeleton; posting rules deferred).</summary>
/// <param name="EntryId">Entry id.</param>
/// <param name="AccountId">Account.</param>
/// <param name="FirmId">Owning firm.</param>
/// <param name="Side">Debit or credit.</param>
/// <param name="Amount">Absolute amount.</param>
/// <param name="Date">Booking date.</param>
/// <param name="Memo">Optional memo.</param>
public sealed record LedgerEntry(
  Guid EntryId,
  AccountId AccountId,
  FirmId FirmId,
  LedgerSide Side,
  Money Amount,
  SimulationDate Date,
  string? Memo);

/// <summary>Marker that an accounting period should close.</summary>
/// <param name="FirmId">Firm closing books.</param>
/// <param name="PeriodEnd">Inclusive period end date.</param>
public sealed record AccountingPeriodClose(
  FirmId FirmId,
  SimulationDate PeriodEnd) : IEconomyCommand;

/// <summary>Event that an accounting period closed.</summary>
/// <param name="FirmId">Firm.</param>
/// <param name="PeriodEnd">Period end.</param>
/// <param name="ClosedAt">Hour when close completed.</param>
public sealed record AccountingPeriodClosed(
  FirmId FirmId,
  SimulationDate PeriodEnd,
  SimulationHour ClosedAt) : IEconomyEvent;
