using Novolis.Economy.Core.Finance;

namespace Novolis.Economy.Core.Extensions;

/// <summary>Insight helpers on legal entities (require economy context for claims).</summary>
public static class LegalEntityExtensions
{
    /// <summary>Liquidity position of this entity in <paramref name="state"/>.</summary>
    public static LiquidityPosition Liquidity(this LegalEntity entity, EconomyState state) =>
        Finance.Liquidity.Of(state, entity.Id);

    /// <summary>Simple book solvency in <paramref name="state"/>.</summary>
    public static Money SimpleSolvency(this LegalEntity entity, EconomyState state) =>
        Finance.Liquidity.SimpleSolvency(state, entity.Id);

    /// <summary>Full financial insight in <paramref name="state"/>.</summary>
    public static EntityFinancialInsight ToInsight(this LegalEntity entity, EconomyState state) =>
        state.InsightFor(entity.Id);

    /// <summary>True when due-now exceeds cash + deposits + committed undrawn.</summary>
    public static bool IsIlliquid(this LegalEntity entity, EconomyState state) =>
        entity.Liquidity(state).Surplus.Amount < 0m;
}
