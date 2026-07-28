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
    PeriodScratch Scratch)
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
        Scratch: PeriodScratch.Empty);

    /// <summary>Price key Region|Resource.</summary>
    public static string PriceKey(RegionId region, ResourceId resource) => $"{region}|{resource}";

    /// <summary>Replace flow ledger.</summary>
    public EconomyState WithFlows(PeriodFlowLedger flows) => this with { Flows = flows };
}
