using Novolis.Economy.Core.Finance;

namespace Novolis.Economy.Core.Invariants;

/// <summary>Violation report from invariant checks.</summary>
public sealed record InvariantViolation(string Code, string Message);

/// <summary>Ownership / resource / finance / share / capacity / household rules (SPEC §19).</summary>
public static class InvariantChecker
{
    /// <summary>Collect all violations without throwing.</summary>
    public static IReadOnlyList<InvariantViolation> Check(EconomyState state)
    {
        var list = new List<InvariantViolation>();
        CheckEntities(state, list);
        CheckHoldings(state, list);
        CheckShares(state, list);
        CheckLoans(state, list);
        CheckCapacity(state, list);
        CheckCohorts(state, list);
        CheckDeposits(state, list);
        return list;
    }

    /// <summary>Throw if any invariant fails.</summary>
    public static void AssertAll(EconomyState state)
    {
        var violations = Check(state);
        if (violations.Count == 0)
            return;
        var msg = string.Join("; ", violations.Select(v => $"{v.Code}: {v.Message}"));
        throw new InvalidOperationException($"Economy invariants violated: {msg}");
    }

    private static void CheckEntities(EconomyState state, List<InvariantViolation> list)
    {
        foreach (var e in state.Entities.Values)
        {
            if (e.Cash.Amount < -1e-9m)
                list.Add(new("CASH_NEG", $"Entity {e.Id} has negative cash {e.Cash}."));
        }
    }

    private static void CheckHoldings(EconomyState state, List<InvariantViolation> list)
    {
        foreach (var h in state.Holdings.Values)
        {
            if (h.Quantity < -1e-9m)
                list.Add(new("HOLD_NEG", $"Holding {h.Owner}/{h.RegionId}/{h.ResourceId} negative."));
            if (!state.Entities.ContainsKey(h.Owner))
                list.Add(new("HOLD_OWNER", $"Holding owner {h.Owner} missing."));
            if (!state.Regions.ContainsKey(h.RegionId))
                list.Add(new("HOLD_REGION", $"Holding region {h.RegionId} missing."));
            if (!state.Resources.ContainsKey(h.ResourceId))
                list.Add(new("HOLD_RES", $"Holding resource {h.ResourceId} missing."));
        }

        foreach (var t in state.Transfers)
        {
            if (t.Quantity < -1e-9m)
                list.Add(new("XFER_NEG", $"Transfer quantity negative for {t.Owner}."));
            if (!state.Entities.ContainsKey(t.Owner))
                list.Add(new("XFER_OWNER", $"Transfer owner {t.Owner} missing."));
        }
    }

    private static void CheckShares(EconomyState state, List<InvariantViolation> list)
    {
        foreach (var sc in state.ShareClasses.Values)
        {
            if (!state.Entities.TryGetValue(sc.Issuer, out var issuer))
            {
                list.Add(new("SHARE_ISSUER", $"Share class issuer {sc.Issuer} missing."));
                continue;
            }

            if (!EntityRules.MayIssueShares(issuer.Kind))
                list.Add(new("SHARE_KIND", $"Issuer {sc.Issuer} kind {issuer.Kind} cannot issue shares."));

            if (!ShareMath.IsConsistent(state, sc))
            {
                var held = ShareMath.HeldUnits(state, sc.Issuer, sc.Name);
                list.Add(new(
                    "SHARE_UNITS",
                    $"Class {sc.Name} held {held} + treasury {sc.TreasuryUnits} ≠ issued {sc.IssuedUnits}."));
            }
        }

        foreach (var h in state.ShareHoldings)
        {
            if (h.Units < -1e-9m)
                list.Add(new("SHARE_HOLD_NEG", $"Share holding units negative for {h.Owner}."));
            if (state.Entities.TryGetValue(h.Issuer, out var iss) && !EntityRules.IsOwnable(iss.Kind))
                list.Add(new("SHARE_UNOWNABLE", $"Share issuer {h.Issuer} is unownable kind {iss.Kind}."));
        }
    }

    private static void CheckLoans(EconomyState state, List<InvariantViolation> list)
    {
        foreach (var loan in state.Loans.Values)
        {
            if (loan.PrincipalOutstanding.Amount < -1e-9m)
                list.Add(new("LOAN_NEG", $"Loan {loan.Id} negative principal."));
            if (!state.Entities.ContainsKey(loan.Lender) || !state.Entities.ContainsKey(loan.Borrower))
                list.Add(new("LOAN_PARTY", $"Loan {loan.Id} missing party."));
        }

        foreach (var f in state.CreditFacilities.Values)
        {
            if (f.Drawn.Amount > f.Limit.Amount + 1e-9m)
                list.Add(new("FACILITY_OVER", $"Facility {f.Id} drawn exceeds limit."));
        }
    }

    private static void CheckCapacity(EconomyState state, List<InvariantViolation> list)
    {
        foreach (var region in state.Regions.Values)
        {
            var living = RegionCapacity.OccupiedLiving(state, region.Id);
            if (living > region.LivingCapacity)
                list.Add(new("CAP_LIVE", $"Region {region.Id} living {living} > {region.LivingCapacity}."));

            var prod = RegionCapacity.InstalledProductionSpace(state, region.Id);
            if (prod > region.ProductionCapacity + 1e-9m)
                list.Add(new("CAP_PROD", $"Region {region.Id} production {prod} > {region.ProductionCapacity}."));
        }
    }

    private static void CheckCohorts(EconomyState state, List<InvariantViolation> list)
    {
        foreach (var c in state.Cohorts.Values)
        {
            if (c.HouseholdCount < 0)
                list.Add(new("COHORT_COUNT", $"Cohort {c.Id} negative household count."));
            if (!state.Regions.ContainsKey(c.RegionId))
                list.Add(new("COHORT_REGION", $"Cohort {c.Id} region missing."));
            if (c.HouseholdEntityId is { } hid && !state.Entities.ContainsKey(hid))
                list.Add(new("COHORT_ENTITY", $"Cohort {c.Id} linked entity missing."));
        }
    }

    private static void CheckDeposits(EconomyState state, List<InvariantViolation> list)
    {
        foreach (var d in state.Deposits)
        {
            if (d.Balance.Amount < -1e-9m)
                list.Add(new("DEP_NEG", $"Deposit negative for {d.Depositor}."));
            if (!state.Entities.TryGetValue(d.Bank, out var bank) || bank.Kind != LegalEntityKind.Bank)
                list.Add(new("DEP_BANK", $"Deposit bank {d.Bank} is not a Bank."));
        }
    }
}
