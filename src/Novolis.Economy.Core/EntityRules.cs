namespace Novolis.Economy.Core;

/// <summary>Capability rules for legal entity kinds (SPEC §2).</summary>
public static class EntityRules
{
    /// <summary>Whether this kind may be owned via shares.</summary>
    public static bool IsOwnable(LegalEntityKind kind) =>
        kind is LegalEntityKind.Firm or LegalEntityKind.Bank or LegalEntityKind.Lender or LegalEntityKind.Insurer;

    /// <summary>Whether this kind may issue share classes.</summary>
    public static bool MayIssueShares(LegalEntityKind kind) => IsOwnable(kind);

    /// <summary>Whether this kind may operate productive activities.</summary>
    public static bool MayOperateActivity(LegalEntityKind kind) => kind == LegalEntityKind.Firm;

    /// <summary>Whether this kind may accept deposits.</summary>
    public static bool MayAcceptDeposits(LegalEntityKind kind) => kind == LegalEntityKind.Bank;

    /// <summary>Whether this kind may underwrite insurance.</summary>
    public static bool MayInsure(LegalEntityKind kind) => kind == LegalEntityKind.Insurer;

    /// <summary>Whether this kind sets fiscal/regulatory policy.</summary>
    public static bool IsPolicyAuthority(LegalEntityKind kind) => kind == LegalEntityKind.State;

    /// <summary>Households cannot be share issuers or appear as ShareHolding.Issuer.</summary>
    public static void EnsureMayIssueShares(LegalEntity entity)
    {
        if (!MayIssueShares(entity.Kind))
            throw new InvalidOperationException($"{entity.Kind} cannot issue shares.");
    }

    /// <summary>Households are unownable.</summary>
    public static void EnsureOwnableIssuer(LegalEntity issuer)
    {
        if (!IsOwnable(issuer.Kind))
            throw new InvalidOperationException($"{issuer.Kind} cannot be an ownership target.");
    }
}
