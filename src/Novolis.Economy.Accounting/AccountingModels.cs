using System.Collections.Immutable;
using Novolis.Economy;

namespace Novolis.Economy.Accounting;

/// <summary>Standard firm chart-of-accounts roles.</summary>
public enum AccountRole
{
  /// <summary>Cash / bank.</summary>
  Cash = 0,
  /// <summary>Inventory asset.</summary>
  Inventory = 1,
  /// <summary>Accounts receivable.</summary>
  AccountsReceivable = 2,
  /// <summary>Accounts payable.</summary>
  AccountsPayable = 3,
  /// <summary>Sales revenue.</summary>
  Revenue = 4,
  /// <summary>Cost of goods sold.</summary>
  CostOfGoodsSold = 5,
  /// <summary>Wage expense.</summary>
  WageExpense = 6,
  /// <summary>Owner equity / retained earnings.</summary>
  Equity = 7,
  /// <summary>Wage liability accrued.</summary>
  WagesPayable = 8,
}

/// <summary>Identifies a ledger account.</summary>
public readonly record struct AccountId(Guid Value)
{
  /// <summary>Creates a new account id.</summary>
  public static AccountId New() => new(Guid.NewGuid());

  /// <summary>Creates an account id from a fixed guid.</summary>
  public static AccountId From(Guid value) => new(value);

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

/// <summary>Single ledger line.</summary>
public sealed record LedgerEntry(
  Guid EntryId,
  AccountId AccountId,
  FirmId FirmId,
  LedgerSide Side,
  Money Amount,
  SimulationDate Date,
  string? Memo);

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

/// <summary>Per-firm ledger balances and chart.</summary>
public sealed class FirmLedger
{
  private readonly Dictionary<AccountRole, AccountId> _roles = new();
  private readonly Dictionary<AccountId, Money> _balances = new();
  private readonly List<LedgerEntry> _entries = [];

  /// <summary>Creates an empty ledger with a standard chart for the firm.</summary>
  public FirmLedger(FirmId firmId)
  {
    FirmId = firmId;
    foreach (AccountRole role in Enum.GetValues<AccountRole>())
    {
      var id = AccountId.From(CreateRoleGuid(firmId, role));
      _roles[role] = id;
      _balances[id] = Money.Zero;
    }
  }

  /// <summary>Owning firm.</summary>
  public FirmId FirmId { get; }

  /// <summary>Posted entries.</summary>
  public IReadOnlyList<LedgerEntry> Entries => _entries;

  /// <summary>Account id for a role.</summary>
  public AccountId Account(AccountRole role) => _roles[role];

  /// <summary>Balance for a role (signed: debit-positive for assets/expenses).</summary>
  public Money Balance(AccountRole role) => _balances[_roles[role]];

  /// <summary>Cash balance.</summary>
  public Money Cash => Balance(AccountRole.Cash);

  /// <summary>Posts a balanced double-entry pair.</summary>
  public void Post(
    AccountRole debit,
    AccountRole credit,
    Money amount,
    SimulationDate date,
    string? memo,
    List<IEconomyEvent>? events = null)
  {
    if (amount.Amount <= 0m)
    {
      return;
    }

    var entryId = CreateEntryGuid(FirmId, _entries.Count);

    var debitAccount = _roles[debit];
    var creditAccount = _roles[credit];
    _balances[debitAccount] = _balances[debitAccount] + amount;
    _balances[creditAccount] = _balances[creditAccount] - amount;
    _entries.Add(new LedgerEntry(entryId, debitAccount, FirmId, LedgerSide.Debit, amount, date, memo));
    _entries.Add(new LedgerEntry(
      CreateEntryGuid(FirmId, _entries.Count),
      creditAccount,
      FirmId,
      LedgerSide.Credit,
      amount,
      date,
      memo));
  }

  /// <summary>Seeds opening cash against equity.</summary>
  public void SeedCash(Money amount, SimulationDate date)
  {
    Post(AccountRole.Cash, AccountRole.Equity, amount, date, "Opening cash");
  }

  /// <summary>Seeds opening inventory against equity.</summary>
  public void SeedInventory(Money amount, SimulationDate date)
  {
    Post(AccountRole.Inventory, AccountRole.Equity, amount, date, "Opening inventory");
  }

  /// <summary>Fingerprint for world hashing.</summary>
  public ulong Fingerprint()
  {
    const ulong offset = 14695981039346656037UL;
    const ulong prime = 1099511628211UL;
    var hash = offset;
    foreach (AccountRole role in Enum.GetValues<AccountRole>().OrderBy(r => (int)r))
    {
      hash = (hash ^ (ulong)role) * prime;
      var bits = decimal.GetBits(Balance(role).Amount);
      foreach (var b in bits)
      {
        hash = (hash ^ (ulong)(uint)b) * prime;
      }
    }

    hash = (hash ^ (ulong)_entries.Count) * prime;
    return hash;
  }

  private static Guid CreateRoleGuid(FirmId firmId, AccountRole role)
  {
    var bytes = firmId.Value.ToByteArray();
    bytes[0] = (byte)role;
    bytes[1] = 0xAC;
    return new Guid(bytes);
  }

  private static Guid CreateEntryGuid(FirmId firmId, int index)
  {
    var bytes = firmId.Value.ToByteArray();
    var idx = BitConverter.GetBytes(index);
    Buffer.BlockCopy(idx, 0, bytes, 12, 4);
    bytes[15] = 0xEE;
    return new Guid(bytes);
  }
}

/// <summary>Double-entry posting helpers for commerce flows.</summary>
public static class LedgerEngine
{
  /// <summary>Records purchase of inventory for cash.</summary>
  public static void PostCashPurchase(FirmLedger ledger, Money amount, SimulationDate date) =>
    ledger.Post(AccountRole.Inventory, AccountRole.Cash, amount, date, "Cash purchase");

  /// <summary>Records sale: debit cash, credit revenue; debit COGS, credit inventory.</summary>
  public static void PostCashSale(
    FirmLedger ledger,
    Money revenue,
    Money cogs,
    SimulationDate date)
  {
    ledger.Post(AccountRole.Cash, AccountRole.Revenue, revenue, date, "Cash sale");
    if (cogs.Amount > 0m)
    {
      ledger.Post(AccountRole.CostOfGoodsSold, AccountRole.Inventory, cogs, date, "COGS");
    }
  }

  /// <summary>Accrues wages.</summary>
  public static void AccrueWages(FirmLedger ledger, Money amount, SimulationDate date) =>
    ledger.Post(AccountRole.WageExpense, AccountRole.WagesPayable, amount, date, "Wage accrual");

  /// <summary>Pays accrued wages from cash.</summary>
  public static void PayWages(FirmLedger ledger, Money amount, SimulationDate date) =>
    ledger.Post(AccountRole.WagesPayable, AccountRole.Cash, amount, date, "Wage payment");

  /// <summary>Writes off spoiled inventory to COGS.</summary>
  public static void WriteOffInventory(FirmLedger ledger, Money amount, SimulationDate date) =>
    ledger.Post(AccountRole.CostOfGoodsSold, AccountRole.Inventory, amount, date, "Spoilage");
}

/// <summary>Marker that an accounting period should close.</summary>
public sealed record AccountingPeriodClose(
  FirmId FirmId,
  SimulationDate PeriodEnd) : IEconomyCommand;

/// <summary>Event that an accounting period closed.</summary>
public sealed record AccountingPeriodClosed(
  FirmId FirmId,
  SimulationDate PeriodEnd,
  SimulationHour ClosedAt) : IEconomyEvent;
