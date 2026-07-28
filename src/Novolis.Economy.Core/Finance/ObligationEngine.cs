namespace Novolis.Economy.Core.Finance;

/// <summary>Payment obligation create + priority settle (SPEC §13).</summary>
public static class ObligationEngine
{
    /// <summary>Settlement priority (lower = first).</summary>
    public static int Priority(ObligationKind kind) =>
        kind switch
        {
            ObligationKind.Wage => 0,
            ObligationKind.Tax => 1,
            ObligationKind.Interest => 2,
            ObligationKind.Principal => 3,
            ObligationKind.InsurancePremium => 4,
            ObligationKind.InsuranceClaim => 5,
            ObligationKind.Dividend => 6,
            ObligationKind.Trade => 7,
            _ => 99
        };

    /// <summary>Create a pending obligation.</summary>
    public static EconomyState Create(
        EconomyState state,
        LegalEntityId debtor,
        LegalEntityId creditor,
        Money amount,
        int duePeriod,
        ObligationKind kind)
    {
        if (amount.Amount <= 0m)
            return state;
        var id = ObligationId.New();
        var list = new List<PaymentObligation>(state.Obligations)
        {
            new(id, debtor, creditor, amount, duePeriod, kind, ObligationStatus.Pending)
        };
        return state with { Obligations = list };
    }

    /// <summary>
    /// Settle obligations due at or before current period, by priority then amount.
    /// Uses cash first, then deposits (simple liquidity). Marks unpaid as Delinquent.
    /// </summary>
    public static EconomyState SettleDue(EconomyState state)
    {
        var period = state.Period;
        var due = state.Obligations
            .Select((o, i) => (o, i))
            .Where(t => t.o.Status == ObligationStatus.Pending && t.o.DuePeriod <= period)
            .OrderBy(t => Priority(t.o.Kind))
            .ThenBy(t => t.o.Amount.Amount)
            .ToList();

        var obligations = new List<PaymentObligation>(state.Obligations);
        foreach (var (ob, _) in due)
        {
            var idx = obligations.FindIndex(x => x.Id.Equals(ob.Id));
            if (idx < 0)
                continue;

            if (TryPay(ref state, ob.Debtor, ob.Creditor, ob.Amount))
            {
                obligations[idx] = ob with { Status = ObligationStatus.Paid };
                state = state.WithFlows(state.Flows.RecordObligationPaid(ob.Amount));
            }
            else
            {
                obligations[idx] = ob with { Status = ObligationStatus.Delinquent };
            }
        }

        return state with { Obligations = obligations };
    }

    private static bool TryPay(ref EconomyState state, LegalEntityId debtor, LegalEntityId creditor, Money amount)
    {
        if (CashLedger.TryDebit(ref state, debtor, amount))
        {
            state = CashLedger.Credit(state, creditor, amount);
            return true;
        }

        return DepositLedger.TryPayFromDeposits(ref state, debtor, creditor, amount);
    }
}
