namespace Novolis.Economy.Finance.Extensions;

/// <summary>Aggregate inter-firm loan book.</summary>
public sealed record LoanBookSnapshot(
    int ActiveCount,
    int DefaultedCount,
    int ClosedCount,
    Money PrincipalOutstanding,
    Money AccruedInterestTotal,
    IReadOnlyList<LoanInsight> Loans);
