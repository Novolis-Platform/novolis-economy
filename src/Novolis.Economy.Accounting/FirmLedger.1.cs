using System.Collections.Immutable;
using Novolis.Economy;
using Novolis.Economy.Abstractions;

namespace Novolis.Economy.Accounting;

/// <summary>Per-firm ledger balances and chart.</summary>
public sealed class FirmLedger : ILoanLedger<SimulationDate>
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

  /// <summary>
  /// Raised before a posting changes the cash balance. Simulation attaches
  /// this hook to project the change into Core's monetary position.
  /// </summary>
  public event Action<Money>? CashChanging;

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
    var cashDelta =
      (debit == AccountRole.Cash ? amount : Money.Zero) -
      (credit == AccountRole.Cash ? amount : Money.Zero);
    if (Cash.Amount + cashDelta.Amount < -0.0000001m)
    {
      throw new InvalidOperationException(
        $"Cash balance for {FirmId} cannot become negative: " +
        $"current {Cash}, delta {cashDelta}.");
    }

    if (cashDelta.Amount != 0m)
      CashChanging?.Invoke(cashDelta);

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

  /// <inheritdoc />
  public void PostLoanDisbursement(
    ILoanLedger<SimulationDate> borrower,
    Money principal,
    SimulationDate date) =>
    LedgerEngine.PostLoanDisbursement(
      this,
      RequireFirmLedger(borrower),
      principal,
      date);

  /// <inheritdoc />
  public void PostHouseholdLoanDisbursement(
    ILoanLedger<SimulationDate> borrower,
    Money principal,
    SimulationDate date) =>
    LedgerEngine.PostHouseholdLoanDisbursement(
      this,
      RequireFirmLedger(borrower),
      principal,
      date);

  /// <inheritdoc />
  public void PostInterestAccrual(
    ILoanLedger<SimulationDate> borrower,
    Money interest,
    SimulationDate date) =>
    LedgerEngine.PostInterestAccrual(
      this,
      RequireFirmLedger(borrower),
      interest,
      date);

  /// <inheritdoc />
  public void PostLoanRepayment(
    ILoanLedger<SimulationDate> borrower,
    Money amount,
    SimulationDate date) =>
    LedgerEngine.PostLoanRepayment(
      this,
      RequireFirmLedger(borrower),
      amount,
      date);

  /// <inheritdoc />
  public void PostHouseholdLoanRepayment(
    ILoanLedger<SimulationDate> borrower,
    Money amount,
    SimulationDate date) =>
    LedgerEngine.PostHouseholdLoanRepayment(
      this,
      RequireFirmLedger(borrower),
      amount,
      date);

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

  private static FirmLedger RequireFirmLedger(ILoanLedger<SimulationDate> ledger) =>
    ledger as FirmLedger
    ?? throw new ArgumentException(
      "Loan posting requires the Accounting FirmLedger adapter.",
      nameof(ledger));
}
