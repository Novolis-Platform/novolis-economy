namespace Novolis.Economy.Models.SmallOpenRegionalTrade;

/// <summary>Demand rule selected by the flagship model specification.</summary>
public enum FoodDemandMechanism
{
    /// <summary>Allocate a fixed cohort budget envelope to food.</summary>
    CohortBudgetShare = 0,

    /// <summary>Protect subsistence consumption before discretionary demand.</summary>
    LinearExpenditure = 1
}
