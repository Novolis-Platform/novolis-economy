namespace Novolis.Economy.Models.SmallOpenRegionalTrade;

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
