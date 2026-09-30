namespace Novolis.Economy.Models.SmallOpenRegionalTrade;

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
