using System.Security.Cryptography;
using System.Text;

namespace Novolis.Economy.Core;

/// <summary>Stable ids derived from simulation state rather than ambient randomness.</summary>
public static class DeterministicIds
{
    /// <summary>Derive a repeatable Guid from a labeled sequence of values.</summary>
    public static Guid GuidFor(string kind, params object?[] values)
    {
        ArgumentException.ThrowIfNullOrEmpty(kind);
        var input = string.Join(
            "|",
            new[] { kind }.Concat(values.Select(value => value?.ToString() ?? "<null>")));
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return new Guid(digest.AsSpan(0, 16));
    }

    /// <summary>Derive a repeatable loan identity for one Core transition.</summary>
    public static LoanId LoanIdFor(
        EconomyState state,
        LegalEntityId lender,
        LegalEntityId borrower)
    {
        var sequence = state.Loans.Count;
        var id = LoanId.From(GuidFor(
            "loan",
            state.SimulationSeed,
            state.Period,
            state.TransitionSequence,
            lender.Value,
            borrower.Value,
            sequence));
        while (state.Loans.ContainsKey(id))
        {
            sequence++;
            id = LoanId.From(GuidFor(
                "loan",
                state.SimulationSeed,
                state.Period,
                state.TransitionSequence,
                lender.Value,
                borrower.Value,
                sequence));
        }

        return id;
    }

    /// <summary>Derive a repeatable obligation identity.</summary>
    public static ObligationId ObligationIdFor(
        EconomyState state,
        LegalEntityId debtor,
        LegalEntityId creditor,
        Money amount,
        int duePeriod,
        ObligationKind kind)
    {
        var sequence = state.Obligations.Count;
        var id = ObligationId.From(GuidFor(
            "obligation",
            state.SimulationSeed,
            state.Period,
            state.TransitionSequence,
            debtor.Value,
            creditor.Value,
            amount.Amount,
            duePeriod,
            kind,
            sequence));
        while (state.Obligations.Any(obligation => obligation.Id.Equals(id)))
        {
            sequence++;
            id = ObligationId.From(GuidFor(
                "obligation",
                state.SimulationSeed,
                state.Period,
                state.TransitionSequence,
                debtor.Value,
                creditor.Value,
                amount.Amount,
                duePeriod,
                kind,
                sequence));
        }

        return id;
    }

    /// <summary>Derive a repeatable cohort id for a migration split.</summary>
    public static CohortId CohortSplitIdFor(
        EconomyState state,
        CohortId source,
        RegionId destination,
        int movedHouseholds)
    {
        var sequence = state.Cohorts.Count;
        var id = CohortId.From(GuidFor(
            "cohort-split",
            state.SimulationSeed,
            state.Period,
            state.TransitionSequence,
            source.Value,
            destination.Value,
            movedHouseholds,
            sequence));
        while (state.Cohorts.ContainsKey(id))
        {
            sequence++;
            id = CohortId.From(GuidFor(
                "cohort-split",
                state.SimulationSeed,
                state.Period,
                state.TransitionSequence,
                source.Value,
                destination.Value,
                movedHouseholds,
                sequence));
        }

        return id;
    }
}
