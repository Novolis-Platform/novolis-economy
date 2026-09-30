namespace Novolis.Economy.Models.SmallOpenRegionalTrade;

/// <summary>Credit behavior selected by the flagship model.</summary>
public enum CreditMechanism
{
    /// <summary>No working-capital draw is accepted.</summary>
    Disabled = 0,

    /// <summary>Use the declared bounded working-capital facility.</summary>
    BoundedWorkingCapital = 1
}
