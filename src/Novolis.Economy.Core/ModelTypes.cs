namespace Novolis.Economy.Core;

/// <summary>Party that may own assets, owe obligations, and transact (SPEC §2).</summary>
public sealed record LegalEntity(
    LegalEntityId Id,
    LegalEntityKind Kind,
    Money Cash);

/// <summary>Homogeneous economic point (SPEC §3).</summary>
public sealed record Region(
    RegionId Id,
    int LivingCapacity,
    decimal ProductionCapacity,
    decimal LogisticsCapacity);

/// <summary>Behavioral profile for a cohort (SPEC §4).</summary>
public sealed record HouseholdProfile(
    decimal ConsumptionWeight,
    decimal SavingsPreference,
    decimal LaborQuality,
    decimal MigrationPreference);

/// <summary>
/// Aggregate of similar households (SPEC §4).
/// <paramref name="HouseholdEntityId"/> is a Core extension linking the cohort to a Household legal entity for wages/dividends/claims.
/// </summary>
public sealed record HouseholdCohort(
    CohortId Id,
    RegionId RegionId,
    int HouseholdCount,
    HouseholdProfile Profile,
    HouseholdLaborKind LaborKind,
    Money CashPerHousehold,
    LegalEntityId? HouseholdEntityId = null);

/// <summary>Named resource type (SPEC §7).</summary>
public sealed record Resource(
    ResourceId Id,
    string Name,
    ResourceKind Kind);

/// <summary>Quantity of a resource.</summary>
public sealed record ResourceAmount(ResourceId ResourceId, decimal Quantity);

/// <summary>Transform recipe for one activity run (SPEC §6).</summary>
public sealed record ActivityRecipe(
    IReadOnlyList<ResourceAmount> Inputs,
    IReadOnlyList<ResourceAmount> Outputs,
    decimal LaborHoursPerRun,
    decimal ProductionSpacePerRun);

/// <summary>Productive unit operated by a firm in a region (SPEC §6).</summary>
public sealed record Activity(
    ActivityId Id,
    LegalEntityId Operator,
    RegionId RegionId,
    ActivityRecipe Recipe,
    decimal InstalledCapacity);

/// <summary>Who owns how much of which resource, and where (SPEC §8).</summary>
public sealed record ResourceHolding(
    LegalEntityId Owner,
    RegionId RegionId,
    ResourceId ResourceId,
    decimal Quantity);

/// <summary>Lane between regions (SPEC §9).</summary>
public sealed record TransportLane(
    RegionId Origin,
    RegionId Destination,
    int TravelPeriods,
    decimal CapacityPerPeriod);

/// <summary>In-flight movement preserving ownership unless a sale occurs (SPEC §9).</summary>
public sealed record ResourceTransfer(
    LegalEntityId Owner,
    ResourceId ResourceId,
    decimal Quantity,
    RegionId Origin,
    RegionId Destination,
    int RemainingPeriods);

/// <summary>Issued share class (SPEC §10).</summary>
public sealed record ShareClass(
    LegalEntityId Issuer,
    string Name,
    decimal IssuedUnits,
    decimal VotesPerUnit,
    decimal TreasuryUnits = 0m);

/// <summary>Units of a named share class held by an owner (SPEC §10).</summary>
public sealed record ShareHolding(
    LegalEntityId Owner,
    LegalEntityId Issuer,
    string ShareClass,
    decimal Units);

/// <summary>Existing debt instrument (SPEC §11).</summary>
public sealed record Loan(
    LoanId Id,
    LegalEntityId Lender,
    LegalEntityId Borrower,
    Money PrincipalOutstanding,
    decimal InterestRatePerPeriod,
    int RemainingPeriods,
    LoanStatus Status);

/// <summary>Capacity to create debt (SPEC §12).</summary>
public sealed record CreditFacility(
    CreditFacilityId Id,
    LegalEntityId Provider,
    LegalEntityId Borrower,
    Money Limit,
    Money Drawn,
    bool IsCommitted)
{
    /// <summary>Undrawn capacity.</summary>
    public Money Available => Money.From(Math.Max(0m, Limit.Amount - Drawn.Amount));
}

/// <summary>Timed payment claim (SPEC §13).</summary>
public sealed record PaymentObligation(
    ObligationId Id,
    LegalEntityId Debtor,
    LegalEntityId Creditor,
    Money Amount,
    int DuePeriod,
    ObligationKind Kind,
    ObligationStatus Status);

/// <summary>Derived ability to meet due obligations (SPEC §14).</summary>
public sealed record LiquidityPosition(
    Money Cash,
    Money AccessibleDeposits,
    Money UndrawnCommittedCredit,
    Money DueNow)
{
    public Money Available => Cash + AccessibleDeposits + UndrawnCommittedCredit;
    public Money Surplus => Available - DueNow;
}

/// <summary>Deposit claim against a bank (SPEC §15).</summary>
public sealed record Deposit(
    LegalEntityId Depositor,
    LegalEntityId Bank,
    Money Balance);

/// <summary>Insurance contract (SPEC §16).</summary>
public sealed record InsuranceCoverage(
    LegalEntityId Insurer,
    LegalEntityId Insured,
    RiskKind Risk,
    decimal CoveredFraction,
    Money Deductible,
    Money PremiumPerPeriod);

/// <summary>State rules that alter flows (SPEC §17).</summary>
public sealed record StatePolicy(
    decimal HouseholdTaxRate,
    decimal FirmTaxRate,
    Money TransferPerHousehold,
    decimal DepositReserveRequirement,
    decimal InsuranceCapitalRequirement,
    Money WagePerLaborHour)
{
    /// <summary>Zero rates / transfers.</summary>
    public static StatePolicy Neutral { get; } = new(0m, 0m, Money.Zero, 0m, 0m, Money.From(1m));
}

/// <summary>Posted unit price for matching (no order book; SPEC §20 / §22).</summary>
public sealed record PostedPrice(
    RegionId RegionId,
    ResourceId ResourceId,
    Money UnitPrice);

/// <summary>Pending loss events applied during the insurance step.</summary>
public sealed record LossEvent(
    LegalEntityId Insured,
    RiskKind Risk,
    Money GrossLoss);
