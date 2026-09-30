using Novolis.Economy.Core;
using Novolis.Economy.Core.Finance;
using Novolis.Economy.Core.Holdings;
using Novolis.Economy.Core.Invariants;
using Novolis.Economy.Core.Labor;
using Novolis.Economy.Core.Production;
using Novolis.Economy.Core.Transport;
using Novolis.Economy.Core.Transactions;
using CoreLegalEntityKind = Novolis.Economy.Core.LegalEntityKind;

using Novolis.Economy.Simulation.Bounded;

namespace Novolis.Economy.Simulation.Bounded;

/// <summary>13. Mark delinquency and default.</summary>
public sealed class MarkDelinquencyStep : IBoundedPeriodStep
{
    public string Name => "13_MarkDelinquency";

    public BoundedPeriodState Execute(BoundedPeriodState current)
    {
        var obligations = current.Obligations.ToList();
        for (var i = 0; i < obligations.Count; i++)
        {
            var o = obligations[i];
            if (o.Status != ObligationStatus.Delinquent)
                continue;
            // The declared credit specification controls delinquency duration.
            if (current.Period - o.DuePeriod >=
                current.Specification.Credit.DelinquencyPeriodsBeforeDefault)
                obligations[i] = o with { Status = ObligationStatus.Defaulted };
        }

        var loans = ClaimLedger.LoanView(current)
            .ToDictionary(loan => loan.Id);
        foreach (var loan in loans.Values)
        {
            if (loan.Status is not (LoanStatus.Performing or LoanStatus.Delinquent))
                continue;
            var hasDelinquent = obligations.Any(o =>
                o.Debtor.Equals(loan.Borrower) &&
                o.Creditor.Equals(loan.Lender) &&
                o.Status is ObligationStatus.Delinquent or ObligationStatus.Defaulted &&
                o.Kind is ObligationKind.Interest or ObligationKind.Principal);
            if (!hasDelinquent)
                continue;
            loans[loan.Id] = loan with
            {
                Status = obligations.Any(o =>
                    o.Debtor.Equals(loan.Borrower) && o.Status == ObligationStatus.Defaulted)
                    ? LoanStatus.Defaulted
                    : LoanStatus.Delinquent
            };
        }

        // Mark repaid loans with zero principal and no pending principal/interest
        foreach (var loan in loans.Values.ToList())
        {
            if (loan.PrincipalOutstanding.Amount > 1e-12m)
                continue;
            var pending = obligations.Any(o =>
                o.Debtor.Equals(loan.Borrower) &&
                o.Creditor.Equals(loan.Lender) &&
                o.Status == ObligationStatus.Pending &&
                o.Kind is ObligationKind.Interest or ObligationKind.Principal);
            if (!pending)
                loans[loan.Id] = loan with { Status = LoanStatus.Repaid };
        }

        var next = current with { Obligations = obligations, Loans = loans };
        foreach (var loan in loans.Values)
        {
            var claim = ClaimLedger.Snapshot(next).GetValueOrDefault(
                ClaimLedger.ClaimIdFor(loan.Id));
            if (claim is null || claim.Status == loan.Status)
                continue;
            next = next.WithEconomy(ClaimLedger.Upsert(next, claim with { Status = loan.Status }));
        }

        return next.WithEconomy(ClaimLedger.SyncFromLegacyLoans(next));
    }
}
