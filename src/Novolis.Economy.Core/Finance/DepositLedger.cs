namespace Novolis.Economy.Core.Finance;

/// <summary>Deposit ledger (SPEC §15).</summary>
public static class DepositLedger
{
  private static int IndexOf(IReadOnlyList<Deposit> deposits, LegalEntityId depositor, LegalEntityId bank)
  {
    for (var i = 0; i < deposits.Count; i++)
    {
      if (deposits[i].Depositor.Equals(depositor) && deposits[i].Bank.Equals(bank))
        return i;
    }

    return -1;
  }

  /// <summary>Increase deposit balance (creates row if needed).</summary>
  public static EconomyState Credit(
    EconomyState state,
    LegalEntityId depositor,
    LegalEntityId bank,
    Money amount)
  {
    if (amount.Amount <= 0m)
      return state;
    var bankEntity = state.Entities[bank];
    if (bankEntity.Kind != LegalEntityKind.Bank)
      throw new InvalidOperationException("Only banks accept deposits.");

    var list = new List<Deposit>(state.Deposits);
    var i = IndexOf(list, depositor, bank);
    if (i < 0)
      list.Add(new Deposit(depositor, bank, amount));
    else
      list[i] = list[i] with { Balance = list[i].Balance + amount };
    return state with { Deposits = list };
  }

  /// <summary>Decrease deposit balance.</summary>
  public static EconomyState Debit(
    EconomyState state,
    LegalEntityId depositor,
    LegalEntityId bank,
    Money amount)
  {
    if (amount.Amount <= 0m)
      return state;
    var list = new List<Deposit>(state.Deposits);
    var i = IndexOf(list, depositor, bank);
    if (i < 0 || list[i].Balance.Amount + 1e-12m < amount.Amount)
      throw new InvalidOperationException("Insufficient deposit balance.");
    var next = list[i].Balance - amount;
    if (next.Amount <= 1e-12m)
      list.RemoveAt(i);
    else
      list[i] = list[i] with { Balance = next };
    return state with { Deposits = list };
  }

  /// <summary>Total deposits held by an entity across banks.</summary>
  public static Money TotalFor(EconomyState state, LegalEntityId depositor) =>
    Money.From(state.Deposits.Where(d => d.Depositor.Equals(depositor)).Sum(d => d.Balance.Amount));

  /// <summary>
  /// Decrease deposit and credit creditor: prefer same-bank deposit transfer (no vault cash),
  /// else withdraw to cash when the bank has vault cash.
  /// </summary>
  public static bool TryPayFromDeposits(
    ref EconomyState state,
    LegalEntityId debtor,
    LegalEntityId creditor,
    Money amount)
  {
    if (amount.Amount <= 0m)
      return true;

    var remaining = amount.Amount;
    var deposits = state.Deposits.Where(d => d.Depositor.Equals(debtor)).ToList();
    foreach (var dep in deposits)
    {
      if (remaining <= 1e-12m)
        break;
      var takeAmt = Math.Min(dep.Balance.Amount, remaining);
      if (takeAmt <= 0m)
        continue;
      var take = Money.From(takeAmt);
      state = Debit(state, debtor, dep.Bank, take);
      state = Credit(state, creditor, dep.Bank, take);
      remaining -= takeAmt;
    }

    return remaining <= 1e-12m;
  }
}