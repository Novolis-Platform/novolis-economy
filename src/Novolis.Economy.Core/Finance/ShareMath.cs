namespace Novolis.Economy.Core.Finance;

/// <summary>Share class consistency (SPEC §10).</summary>
public static class ShareMath
{
    /// <summary>Key Issuer|ClassName.</summary>
    public static string ClassKey(LegalEntityId issuer, string name) => $"{issuer}|{name}";

    /// <summary>Σ units held + treasury must equal IssuedUnits.</summary>
    public static decimal HeldUnits(EconomyState state, LegalEntityId issuer, string shareClass) =>
        state.ShareHoldings
            .Where(h => h.Issuer.Equals(issuer) &&
                        string.Equals(h.ShareClass, shareClass, StringComparison.Ordinal))
            .Sum(h => h.Units);

    /// <summary>True when held + treasury ≈ issued.</summary>
    public static bool IsConsistent(EconomyState state, ShareClass shareClass)
    {
        var held = HeldUnits(state, shareClass.Issuer, shareClass.Name);
        var total = held + shareClass.TreasuryUnits;
        return Math.Abs(total - shareClass.IssuedUnits) < 1e-9m;
    }

    /// <summary>Upsert a share holding by owner×issuer×class.</summary>
    public static EconomyState UpsertHolding(
        EconomyState state,
        LegalEntityId owner,
        LegalEntityId issuer,
        string shareClass,
        decimal units)
    {
        var list = state.ShareHoldings
            .Where(h => !(h.Owner.Equals(owner) && h.Issuer.Equals(issuer) &&
                          string.Equals(h.ShareClass, shareClass, StringComparison.Ordinal)))
            .ToList();
        if (units > 0m)
            list.Add(new ShareHolding(owner, issuer, shareClass, units));
        return state with { ShareHoldings = list };
    }
}
