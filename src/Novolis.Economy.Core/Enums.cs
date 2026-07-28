namespace Novolis.Economy.Core;

/// <summary>Institutional role of a legal entity (SPEC §2).</summary>
public enum LegalEntityKind
{
    /// <summary>Unownable private beneficiary; supplies labor and consumes.</summary>
    Household = 0,

    /// <summary>Ownable commercial operator; may issue shares and run activities.</summary>
    Firm,

    /// <summary>Extends credit from owned or borrowed funds (not a bank).</summary>
    Lender,

    /// <summary>Accepts deposits; may create deposit liabilities when lending.</summary>
    Bank,

    /// <summary>Receives premiums and accepts specified risks.</summary>
    Insurer,

    /// <summary>Policy authority and fiscal actor.</summary>
    State
}

/// <summary>Labor-hours per household-day — capacity, not productivity (SPEC §5).</summary>
public enum HouseholdLaborKind
{
    Common = 0,
    Mean,
    Extreme
}

/// <summary>Maps labor kind to hours per household-day.</summary>
public static class HouseholdLabor
{
    /// <summary>Labor-hours one average household supplies per day.</summary>
    public static decimal HoursPerDay(HouseholdLaborKind kind) =>
        kind switch
        {
            HouseholdLaborKind.Common => 12m,
            HouseholdLaborKind.Mean => 18m,
            HouseholdLaborKind.Extreme => 24m,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
}

/// <summary>Coarse resource classification (SPEC §7).</summary>
public enum ResourceKind
{
    ConsumerGood = 0,
    IntermediateGood,
    CapitalGood,
    Service
}

/// <summary>Lifecycle of an outstanding loan (SPEC §11).</summary>
public enum LoanStatus
{
    Performing = 0,
    Delinquent,
    Defaulted,
    Repaid
}

/// <summary>Payment obligation kind (SPEC §13).</summary>
public enum ObligationKind
{
    Trade = 0,
    Wage,
    Tax,
    Interest,
    Principal,
    Dividend,
    InsurancePremium,
    InsuranceClaim
}

/// <summary>Payment obligation status (SPEC §13).</summary>
public enum ObligationStatus
{
    Pending = 0,
    Paid,
    Delinquent,
    Defaulted
}

/// <summary>Insured risk category (SPEC §16).</summary>
public enum RiskKind
{
    ProductionLoss = 0,
    TransportLoss,
    LiabilityLoss,
    CreditLoss
}
