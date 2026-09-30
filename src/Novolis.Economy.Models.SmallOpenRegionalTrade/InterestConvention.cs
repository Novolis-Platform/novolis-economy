namespace Novolis.Economy.Models.SmallOpenRegionalTrade;

/// <summary>Convention used when accruing a claim's periodic interest.</summary>
public enum InterestConvention
{
    /// <summary>Apply interest to the current outstanding principal.</summary>
    CompoundPerPeriod = 0,

    /// <summary>Apply interest to the claim's original principal.</summary>
    SimplePerPeriod = 1
}
