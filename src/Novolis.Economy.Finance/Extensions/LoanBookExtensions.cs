namespace Novolis.Economy.Finance.Extensions;

/// <summary>Read-only loan book insights.</summary>
public static class LoanBookExtensions
{
    /// <summary>Per-loan insight.</summary>
    public static LoanInsight ToInsight(this Loan loan) =>
        new(
            loan.Id,
            loan.LenderFirmId,
            loan.BorrowerFirmId,
            loan.Status,
            loan.PrincipalRemaining,
            loan.AccruedInterest,
            loan.AnnualInterestRate,
            loan.OriginatedAt,
            loan.DueAt);

    /// <summary>Aggregate loan-book snapshot.</summary>
    public static LoanBookSnapshot Snapshot(this IEnumerable<Loan> loans)
    {
        var list = loans.Select(l => l.ToInsight()).OrderBy(l => l.Id.Value).ToList();
        return new LoanBookSnapshot(
            ActiveCount: list.Count(l => l.Status == LoanStatus.Active),
            DefaultedCount: list.Count(l => l.Status == LoanStatus.Defaulted),
            ClosedCount: list.Count(l => l.Status == LoanStatus.Closed),
            PrincipalOutstanding: Money.From(
                list.Where(l => l.Status is LoanStatus.Active or LoanStatus.Defaulted)
                    .Sum(l => l.PrincipalRemaining.Amount)),
            AccruedInterestTotal: Money.From(list.Sum(l => l.AccruedInterest.Amount)),
            Loans: list);
    }
}
