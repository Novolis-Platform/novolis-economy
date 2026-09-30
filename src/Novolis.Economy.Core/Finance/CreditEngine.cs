using Novolis.Economy.Core.Transactions;

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
        var loanId = DeterministicIds.LoanIdFor(
            state,
            facility.Provider,
            facility.Borrower);
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
        state = EconomicTransactionEngine.Apply(
            state,
            EconomicTransaction.Create(
                state,
                [new CreateClaim(ClaimLedger.FromLoan(state, loans[loanId]))],
                "loan-origination"));

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
        var loanId = DeterministicIds.LoanIdFor(state, lenderId, borrowerId);
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
        state = EconomicTransactionEngine.Apply(
            state,
            EconomicTransaction.Create(
                state,
                [new CreateClaim(ClaimLedger.FromLoan(state, loans[loanId]))],
                "loan-origination"));

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
        var loan = ClaimLedger.LoanView(state).FirstOrDefault(candidate => candidate.Id.Equals(loanId));
        if (loan is null)
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
        state = state with { Loans = loans };
        return EconomicTransactionEngine.Apply(
            state,
            EconomicTransaction.Create(
                state,
                [
                    new SettleClaim(
                        ClaimLedger.ClaimIdFor(loanId),
                        new AssetAmount(state.MonetaryAssetId, pay.Amount))
                ],
                "loan-principal-settlement"));
    }
}