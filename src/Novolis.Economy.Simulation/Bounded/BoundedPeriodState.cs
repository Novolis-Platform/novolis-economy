using Novolis.Economy.Core;
using Novolis.Economy.Core.Transactions;
using CoreLegalEntity = Novolis.Economy.Core.LegalEntity;
using CoreLegalEntityId = Novolis.Economy.Core.LegalEntityId;

namespace Novolis.Economy.Simulation.Bounded;

/// <summary>
/// Simulation-owned state used while running the deterministic bounded profile.
/// The economic state remains Core-owned; scratch is a separate transient value.
/// </summary>
public sealed record BoundedPeriodState
{
  /// <summary>Creates a bounded run state around authoritative Core state.</summary>
  public BoundedPeriodState(
    EconomyState economy,
    BoundedPeriodScratch? scratch = null,
    EconomyModelSpecification? specification = null)
  {
    Economy = economy ?? throw new ArgumentNullException(nameof(economy));
    Scratch = scratch ?? BoundedPeriodScratch.Empty;
    Specification = specification ?? EconomyModelSpecification.Default;
  }

  /// <summary>Authoritative economic state being transitioned.</summary>
  public EconomyState Economy { get; init; }

  /// <summary>Transient bounded-profile scratch.</summary>
  public BoundedPeriodScratch Scratch { get; init; }

  /// <summary>Behavioral parameters selected by the Simulation model.</summary>
  public EconomyModelSpecification Specification { get; init; }

  /// <summary>Current bounded period.</summary>
  public int Period
  {
    get => Economy.Period;
    init => Economy = Economy with { Period = value };
  }

  /// <summary>Period flow totals.</summary>
  public PeriodFlowLedger Flows
  {
    get => Economy.Flows;
    init => Economy = Economy with { Flows = value };
  }

  /// <summary>Registered legal entities.</summary>
  public IReadOnlyDictionary<CoreLegalEntityId, CoreLegalEntity> Entities =>
    Economy.Entities;

  /// <summary>Registered regions.</summary>
  public IReadOnlyDictionary<RegionId, Region> Regions => Economy.Regions;

  /// <summary>Population cohorts.</summary>
  public IReadOnlyDictionary<CohortId, HouseholdCohort> Cohorts
  {
    get => Economy.Cohorts;
    init => Economy = Economy with { Cohorts = value };
  }

  /// <summary>Production activities.</summary>
  public IReadOnlyDictionary<ActivityId, Activity> Activities => Economy.Activities;

  /// <summary>Share ownership positions.</summary>
  public IReadOnlyList<ShareHolding> ShareHoldings => Economy.ShareHoldings;

  /// <summary>Resource definitions.</summary>
  public IReadOnlyDictionary<ResourceId, Resource> Resources => Economy.Resources;

  /// <summary>Loans.</summary>
  public IReadOnlyDictionary<LoanId, Loan> Loans
  {
    get => Economy.Loans;
    init => Economy = Economy with { Loans = value };
  }

  /// <summary>Credit facilities.</summary>
  public IReadOnlyDictionary<CreditFacilityId, CreditFacility> CreditFacilities =>
    Economy.CreditFacilities;

  /// <summary>Payment obligations.</summary>
  public IReadOnlyList<PaymentObligation> Obligations
  {
    get => Economy.Obligations;
    init => Economy = Economy with { Obligations = value };
  }

  /// <summary>Insurance coverage.</summary>
  public IReadOnlyList<InsuranceCoverage> Insurance => Economy.Insurance;

  /// <summary>State policy.</summary>
  public StatePolicy Policy => Economy.Policy;

  /// <summary>Posted prices.</summary>
  public IReadOnlyDictionary<string, PostedPrice> PostedPrices => Economy.PostedPrices;

  /// <summary>Pending losses.</summary>
  public IReadOnlyList<LossEvent> PendingLosses
  {
    get => Economy.PendingLosses;
    init => Economy = Economy with { PendingLosses = value };
  }

  /// <summary>Resource positions in the legacy projection.</summary>
  public IReadOnlyDictionary<string, ResourceHolding> Holdings => Economy.Holdings;

  /// <summary>Authoritative economic positions.</summary>
  public IReadOnlyDictionary<string, EconomicPosition> Positions => Economy.PositionState;

  /// <summary>Monetary asset used by Core.</summary>
  public EconomicAssetId MonetaryAssetId => Economy.MonetaryAssetId;

  /// <summary>Looks up the asset associated with a resource.</summary>
  public EconomicAssetId AssetFor(ResourceId resourceId) => Economy.AssetFor(resourceId);

  /// <summary>Replaces period flow totals.</summary>
  public BoundedPeriodState WithFlows(PeriodFlowLedger flows) =>
    this with { Flows = flows };

  /// <summary>Replaces Core state while preserving Simulation scratch/specification.</summary>
  public BoundedPeriodState WithEconomy(EconomyState economy) =>
    this with { Economy = economy };

  /// <summary>Converts to the authoritative Core state.</summary>
  public static implicit operator EconomyState(BoundedPeriodState state) =>
    state.Economy;

  /// <summary>Wraps Core state for a new bounded period.</summary>
  public static implicit operator BoundedPeriodState(EconomyState state) =>
    new(state);
}
