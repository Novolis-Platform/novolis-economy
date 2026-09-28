namespace Novolis.Economy.Core;

/// <summary>Aggregate state of one Core economy (SPEC §21).</summary>
public sealed record EconomyState(
    int Period,
    IReadOnlyDictionary<LegalEntityId, LegalEntity> Entities,
    IReadOnlyDictionary<RegionId, Region> Regions,
    IReadOnlyDictionary<CohortId, HouseholdCohort> Cohorts,
    IReadOnlyDictionary<ActivityId, Activity> Activities,
    IReadOnlyDictionary<string, ResourceHolding> Holdings,
    IReadOnlyList<ResourceTransfer> Transfers,
    IReadOnlyDictionary<string, ShareClass> ShareClasses,
    IReadOnlyList<ShareHolding> ShareHoldings,
    IReadOnlyDictionary<LoanId, Loan> Loans,
    IReadOnlyDictionary<CreditFacilityId, CreditFacility> CreditFacilities,
    IReadOnlyList<PaymentObligation> Obligations,
    IReadOnlyList<Deposit> Deposits,
    IReadOnlyList<InsuranceCoverage> Insurance,
    StatePolicy Policy,
    IReadOnlyDictionary<ResourceId, Resource> Resources,
    IReadOnlyDictionary<string, TransportLane> Lanes,
    IReadOnlyDictionary<string, PostedPrice> PostedPrices,
    IReadOnlyList<LossEvent> PendingLosses,
    PeriodFlowLedger Flows,
    PeriodScratch Scratch,
    IReadOnlyDictionary<string, EconomicPosition>? Positions = null,
    EconomicAssetId? UnitOfAccountAssetId = null,
    IReadOnlyDictionary<ClaimId, FinancialClaim>? Claims = null,
    ulong SimulationSeed = 0,
    long TransitionSequence = 0,
    EconomyModelSpecification? ModelSpecification = null)
{
    /// <summary>Empty economy at period 0.</summary>
    public static EconomyState Empty { get; } = new(
        Period: 0,
        Entities: new Dictionary<LegalEntityId, LegalEntity>(),
        Regions: new Dictionary<RegionId, Region>(),
        Cohorts: new Dictionary<CohortId, HouseholdCohort>(),
        Activities: new Dictionary<ActivityId, Activity>(),
        Holdings: new Dictionary<string, ResourceHolding>(),
        Transfers: Array.Empty<ResourceTransfer>(),
        ShareClasses: new Dictionary<string, ShareClass>(),
        ShareHoldings: Array.Empty<ShareHolding>(),
        Loans: new Dictionary<LoanId, Loan>(),
        CreditFacilities: new Dictionary<CreditFacilityId, CreditFacility>(),
        Obligations: Array.Empty<PaymentObligation>(),
        Deposits: Array.Empty<Deposit>(),
        Insurance: Array.Empty<InsuranceCoverage>(),
        Policy: StatePolicy.Neutral,
        Resources: new Dictionary<ResourceId, Resource>(),
        Lanes: new Dictionary<string, TransportLane>(),
        PostedPrices: new Dictionary<string, PostedPrice>(),
        PendingLosses: Array.Empty<LossEvent>(),
        Flows: PeriodFlowLedger.Empty,
        Scratch: PeriodScratch.Empty,
        Positions: new Dictionary<string, EconomicPosition>(),
        UnitOfAccountAssetId: DefaultUnitOfAccountAssetId,
        Claims: new Dictionary<ClaimId, FinancialClaim>(),
        SimulationSeed: 0,
        TransitionSequence: 0,
        ModelSpecification: EconomyModelSpecification.Default);

    /// <summary>Behavioral assumptions declared for this Core state.</summary>
    public EconomyModelSpecification Specification =>
        ModelSpecification ?? EconomyModelSpecification.Default;

    /// <summary>Authoritative financial claims, materialized from legacy loans when needed.</summary>
    public IReadOnlyDictionary<ClaimId, FinancialClaim> ClaimState =>
        Claims ?? new Dictionary<ClaimId, FinancialClaim>();

    /// <summary>
    /// Stable compatibility unit of account for scenarios that have not
    /// registered a monetary asset explicitly.
    /// </summary>
    public static EconomicAssetId DefaultUnitOfAccountAssetId { get; } =
        EconomicAssetId.From(Guid.Parse("2e2b4d3c-7b31-4f63-9e6a-6a4d0c8d4e11"));

    /// <summary>Configured monetary asset used by cash positions.</summary>
    public EconomicAssetId MonetaryAssetId =>
        UnitOfAccountAssetId ?? DefaultUnitOfAccountAssetId;

    /// <summary>Register the unit-of-account economic asset for this scenario.</summary>
    public EconomyState WithMonetaryAsset(EconomicAssetId assetId) =>
        this with { UnitOfAccountAssetId = assetId };

    /// <summary>
    /// Authoritative owner × asset × region positions.
    /// A null value is accepted only for source compatibility with pre-positions
    /// callers; Core materializes it before reading or writing positions.
    /// </summary>
    public IReadOnlyDictionary<string, EconomicPosition> PositionState =>
        Positions ?? Empty.Positions!;

    /// <summary>Resolve a resource's economic asset identity.</summary>
    public EconomicAssetId AssetFor(ResourceId resourceId)
    {
        if (Resources.TryGetValue(resourceId, out var resource) &&
            resource.AssetId != default)
        {
            return resource.AssetId;
        }

        // Compatibility for pre-Primitives resource definitions. New scenario
        // definitions must provide Resource.AssetId explicitly.
        return EconomicAssetId.From(resourceId.Value);
    }

    /// <summary>Price key Region|Resource.</summary>
    public static string PriceKey(RegionId region, ResourceId resource) => $"{region}|{resource}";

    /// <summary>Replace flow ledger.</summary>
    public EconomyState WithFlows(PeriodFlowLedger flows) => this with { Flows = flows };
}
