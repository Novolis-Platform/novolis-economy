namespace Novolis.Economy.Models.SmallOpenRegionalTrade;

/// <summary>How the household food market forms a transaction price.</summary>
public enum FoodMarketMechanism
{
    /// <summary>Use the declared posted price and ration scarce supply.</summary>
    PostedPriceRationing = 0,

    /// <summary>Adjust the posted price from declared demand pressure.</summary>
    SupplyDemandPriceDiscovery = 1
}

/// <summary>Demand rule selected by the flagship model specification.</summary>
public enum FoodDemandMechanism
{
    /// <summary>Allocate a fixed cohort budget envelope to food.</summary>
    CohortBudgetShare = 0,

    /// <summary>Protect subsistence consumption before discretionary demand.</summary>
    LinearExpenditure = 1
}

/// <summary>Credit behavior selected by the flagship model.</summary>
public enum CreditMechanism
{
    /// <summary>No working-capital draw is accepted.</summary>
    Disabled = 0,

    /// <summary>Use the declared bounded working-capital facility.</summary>
    BoundedWorkingCapital = 1
}

/// <summary>Convention used when accruing a claim's periodic interest.</summary>
public enum InterestConvention
{
    /// <summary>Apply interest to the current outstanding principal.</summary>
    CompoundPerPeriod = 0,

    /// <summary>Apply interest to the claim's original principal.</summary>
    SimplePerPeriod = 1
}

/// <summary>Action taken when a claim reaches its declared term.</summary>
public enum DefaultResolution
{
    /// <summary>Leave the outstanding claim visible after maturity.</summary>
    KeepOutstanding = 0,

    /// <summary>Mark the outstanding claim as defaulted.</summary>
    MarkDefaultAtMaturity = 1,

    /// <summary>Write the outstanding claim down to zero.</summary>
    WriteOffAtMaturity = 2
}

/// <summary>Declared fiscal base for the model's optional tax mechanism.</summary>
public enum TaxBase
{
    /// <summary>No fiscal transfer is applied by this model.</summary>
    None = 0,

    /// <summary>Tax the change in the entity's monetary position.</summary>
    MonetaryIncome = 1,

    /// <summary>Tax the entity's period operating surplus.</summary>
    Profit = 2,

    /// <summary>Tax monetary consumption flows.</summary>
    Consumption = 3,

    /// <summary>Tax the entity's monetary wealth stock.</summary>
    Wealth = 4
}
