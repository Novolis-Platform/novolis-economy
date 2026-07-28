namespace Novolis.Economy.Core.Finance;

/// <summary>Loan / credit / deposit / obligation operations (SPEC §11–§15).</summary>
public static class CreditEngine
{
    /// <summary>
    /// Draw on a facility: increases Drawn and creates/augments a performing loan.
    /// Bank providers create a matching deposit liability (endogenous money).
    /// Non-bank lenders transfer cash.
    /// </summary>
    public static EconomyState DrawFacility(
        EconomyState state,
        CreditFacilityId facilityId,
        Money amount,
        decimal interestRatePerPeriod,
        int termPeriods)
    {
        if (amount.Amount <= 0m)
            return state;
        if (!state.CreditFacilities.TryGetValue(facilityId, out var facility))
            throw new InvalidOperationException($"Unknown facility {facilityId}.");
        if (amount.Amount > facility.Available.Amount + 1e-12m)
            throw new InvalidOperationException("Draw exceeds available facility.");

        var facilities = new Dictionary<CreditFacilityId, CreditFacility>(state.CreditFacilities)
        {
            [facilityId] = facility with { Drawn = facility.Drawn + amount }
        };
        state = state with { CreditFacilities = facilities };

        var provider = state.Entities[facility.Provider];
        var loanId = LoanId.New();
        var loans = new Dictionary<LoanId, Loan>(state.Loans)
        {
            [loanId] = new Loan(
                loanId,
                facility.Provider,
                facility.Borrower,
                amount,
                interestRatePerPeriod,
                termPeriods,
                LoanStatus.Performing)
        };
        state = state with { Loans = loans };

        if (provider.Kind == LegalEntityKind.Bank)
        {
            // Endogenous money: loan asset + deposit liability
            state = DepositLedger.Credit(state, facility.Borrower, facility.Provider, amount);
            state = state.WithFlows(state.Flows.RecordMoneyCreated(amount));
        }
        else
        {
            state = CashLedger.Transfer(state, facility.Provider, facility.Borrower, amount);
            state = state.WithFlows(state.Flows.RecordCashMoved(amount));
        }

        return state;
    }

    /// <summary>Non-facility direct loan: bank creates deposit; lender transfers cash.</summary>
    public static EconomyState OriginateLoan(
        EconomyState state,
        LegalEntityId lenderId,
        LegalEntityId borrowerId,
        Money principal,
        decimal interestRatePerPeriod,
        int termPeriods)
    {
        if (principal.Amount <= 0m)
            return state;
        var lender = state.Entities[lenderId];
        var loanId = LoanId.New();
        var loans = new Dictionary<LoanId, Loan>(state.Loans)
        {
            [loanId] = new Loan(
                loanId,
                lenderId,
                borrowerId,
                principal,
                interestRatePerPeriod,
                termPeriods,
                LoanStatus.Performing)
        };
        state = state with { Loans = loans };

        if (lender.Kind == LegalEntityKind.Bank)
        {
            state = DepositLedger.Credit(state, borrowerId, lenderId, principal);
            state = state.WithFlows(state.Flows.RecordMoneyCreated(principal));
        }
        else
        {
            state = CashLedger.Transfer(state, lenderId, borrowerId, principal);
            state = state.WithFlows(state.Flows.RecordCashMoved(principal));
        }

        return state;
    }

    /// <summary>
    /// Repay loan principal. Bank loans destroy matching deposits at the lending bank;
    /// non-bank loans transfer cash borrower → lender.
    /// </summary>
    public static EconomyState RepayPrincipal(EconomyState state, LoanId loanId, Money amount)
    {
        if (amount.Amount <= 0m)
            return state;
        if (!state.Loans.TryGetValue(loanId, out var loan))
            throw new InvalidOperationException($"Unknown loan {loanId}.");
        if (loan.Status is LoanStatus.Repaid or LoanStatus.Defaulted)
            throw new InvalidOperationException($"Loan {loanId} is {loan.Status}.");

        var pay = Money.From(Math.Min(amount.Amount, loan.PrincipalOutstanding.Amount));
        if (pay.Amount <= 0m)
            return state;

        var lender = state.Entities[loan.Lender];
        if (lender.Kind == LegalEntityKind.Bank)
        {
            state = DepositLedger.Debit(state, loan.Borrower, loan.Lender, pay);
            state = state.WithFlows(state.Flows.RecordMoneyDestroyed(pay));
        }
        else
        {
            state = CashLedger.Transfer(state, loan.Borrower, loan.Lender, pay);
            state = state.WithFlows(state.Flows.RecordCashMoved(pay));
        }

        var remaining = loan.PrincipalOutstanding - pay;
        var loans = new Dictionary<LoanId, Loan>(state.Loans)
        {
            [loanId] = loan with
            {
                PrincipalOutstanding = remaining,
                Status = remaining.Amount <= 1e-12m ? LoanStatus.Repaid : loan.Status
            }
        };
        return state with { Loans = loans };
    }
}

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
