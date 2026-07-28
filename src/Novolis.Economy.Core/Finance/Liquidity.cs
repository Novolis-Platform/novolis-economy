namespace Novolis.Economy.Core.Finance;

/// <summary>Liquidity and simple solvency (SPEC §14; Minsky).</summary>
public static class Liquidity
{
    /// <summary>Derive liquidity position for an entity.</summary>
    public static LiquidityPosition Of(EconomyState state, LegalEntityId id)
    {
        var cash = state.Entities.TryGetValue(id, out var e) ? e.Cash : Money.Zero;
        var deposits = DepositLedger.TotalFor(state, id);
        var undrawn = Money.From(
            state.CreditFacilities.Values
                .Where(f => f.Borrower.Equals(id) && f.IsCommitted)
                .Sum(f => f.Available.Amount));
        var dueNow = Money.From(
            state.Obligations
                .Where(o => o.Debtor.Equals(id) &&
                            o.Status == ObligationStatus.Pending &&
                            o.DuePeriod <= state.Period)
                .Sum(o => o.Amount.Amount));
        return new LiquidityPosition(cash, deposits, undrawn, dueNow);
    }

    /// <summary>
    /// Simple book solvency: cash + deposits + undrawn committed − loan principal owed.
    /// Holdings book value omitted unless posted prices cover them (optional extension).
    /// </summary>
    public static Money SimpleSolvency(EconomyState state, LegalEntityId id)
    {
        var liq = Of(state, id);
        var loansOwed = Money.From(
            state.Loans.Values
                .Where(l => l.Borrower.Equals(id) && l.Status is LoanStatus.Performing or LoanStatus.Delinquent)
                .Sum(l => l.PrincipalOutstanding.Amount));
        return liq.Cash + liq.AccessibleDeposits + liq.UndrawnCommittedCredit - loansOwed;
    }
}
