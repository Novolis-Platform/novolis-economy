namespace Novolis.Economy.Core.Finance;

/// <summary>
/// Materializes the authoritative Core claim book and maintains the legacy
/// loan projection while callers migrate.
/// </summary>
public static class ClaimLedger
{
    /// <summary>Stable claim identity for a legacy Core loan.</summary>
    public static ClaimId ClaimIdFor(LoanId loanId) => ClaimId.From(loanId.Value);

    /// <summary>Read claims, importing legacy loans only when no claim exists.</summary>
    public static IReadOnlyDictionary<ClaimId, FinancialClaim> Snapshot(EconomyState state)
    {
        var claims = new Dictionary<ClaimId, FinancialClaim>(state.ClaimState);
        foreach (var loan in state.Loans.Values)
        {
            if (loan.PrincipalOutstanding.Amount < 0m)
                continue;

            var claimId = ClaimIdFor(loan.Id);
            claims.TryAdd(
                claimId,
                new FinancialClaim(
                    claimId,
                    EconomicIdentity.For(loan.Lender),
                    EconomicIdentity.For(loan.Borrower),
                    new AssetAmount(state.MonetaryAssetId, loan.PrincipalOutstanding.Amount),
                    loan.InterestRatePerPeriod,
                    loan.RemainingPeriods,
                    loan.Status));
        }

        return claims;
    }

    /// <summary>Return the claim corresponding to a legacy loan id.</summary>
    public static FinancialClaim Get(EconomyState state, LoanId loanId)
    {
        var claimId = ClaimIdFor(loanId);
        if (!Snapshot(state).TryGetValue(claimId, out var claim))
            throw new InvalidOperationException($"Unknown claim for loan {loanId}.");
        return claim;
    }

    /// <summary>Project the authoritative claim book to the legacy loan shape for reads.</summary>
    public static IReadOnlyList<Loan> LoanView(EconomyState state) =>
        Snapshot(state)
            .Values
            .Select(claim => new Loan(
                LoanId.From(claim.Id.Value),
                EconomicIdentity.ToLegalEntityId(claim.Creditor),
                EconomicIdentity.ToLegalEntityId(claim.Debtor),
                Money.From(claim.Principal.Quantity),
                claim.InterestRatePerPeriod,
                claim.RemainingPeriods,
                claim.Status))
            .ToList();

    /// <summary>
    /// Upsert a claim and update the old Core loan projection for one release.
    /// The claim fields are the source of truth.
    /// </summary>
    public static EconomyState Upsert(EconomyState state, FinancialClaim claim)
    {
        var claims = new Dictionary<ClaimId, FinancialClaim>(Snapshot(state))
        {
            [claim.Id] = claim
        };

        var loanId = LoanId.From(claim.Id.Value);
        var loans = new Dictionary<LoanId, Loan>(state.Loans)
        {
            [loanId] = new Loan(
                loanId,
                EconomicIdentity.ToLegalEntityId(claim.Creditor),
                EconomicIdentity.ToLegalEntityId(claim.Debtor),
                Money.From(claim.Principal.Quantity),
                claim.InterestRatePerPeriod,
                claim.RemainingPeriods,
                claim.Status)
        };

        return state with
        {
            Claims = claims,
            Loans = loans
        };
    }

    /// <summary>Convert a legacy loan into its authoritative claim.</summary>
    public static FinancialClaim FromLoan(EconomyState state, Loan loan) =>
        new(
            ClaimIdFor(loan.Id),
            EconomicIdentity.For(loan.Lender),
            EconomicIdentity.For(loan.Borrower),
            new AssetAmount(state.MonetaryAssetId, loan.PrincipalOutstanding.Amount),
            loan.InterestRatePerPeriod,
            loan.RemainingPeriods,
            loan.Status);

    /// <summary>
    /// Import legacy loans that have no claim yet. Existing claims are never
    /// overwritten: the claim book is authoritative.
    /// </summary>
    public static EconomyState SyncFromLegacyLoans(EconomyState state)
    {
        foreach (var loan in state.Loans.Values)
        {
            if (state.ClaimState.ContainsKey(ClaimIdFor(loan.Id)))
                continue;
            state = Upsert(state, FromLoan(state, loan));
        }
        return state;
    }
}
