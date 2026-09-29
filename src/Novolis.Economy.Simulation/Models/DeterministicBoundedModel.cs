using System.Collections.Immutable;
using Novolis.Economy.Core;
using Novolis.Economy.Population;
using Novolis.Economy.Production;

namespace Novolis.Economy.Simulation.Models;

/// <summary>
/// Secondary finite model profile for exact regression and teaching scenarios.
/// Agents are disabled and all behavior is selected for predictable replay.
/// </summary>
public sealed class DeterministicBoundedModel : SimulationModelDefinition
{
  /// <summary>Creates the bounded deterministic profile.</summary>
  public DeterministicBoundedModel(
    EconomyModelSpecification? specification = null)
  {
    Specification = specification ?? EconomyModelSpecification.Default with
    {
      Version = "deterministic-bounded-1"
    };
  }

  /// <inheritdoc />
  public override SimulationModelIdentity Identity =>
    new("DeterministicBounded", Specification.Version);

  /// <inheritdoc />
  public override EconomyModelSpecification Specification { get; }

  /// <inheritdoc />
  public override IReadOnlyList<string> RuleIdentities =>
  [
    "bounded.fixed-identities",
    "bounded.fixed-inputs",
    "bounded.posted-price-rationing",
    "bounded.no-agent-randomness"
  ];

  /// <inheritdoc />
  public override IReadOnlyList<string> AgentProfileIds =>
    ["agents.disabled"];

  /// <inheritdoc />
  public override EconomyWorld CreateWorld(ulong seed) =>
    CreateBoundedWorld();

  private static EconomyWorld CreateBoundedWorld()
  {
    var region = GeographicAreaId.From(Guid.Parse(
      "b1000000-0000-4000-8000-000000000001"));
    var firm = FirmId.From(Guid.Parse(
      "b2000000-0000-4000-8000-000000000001"));
    var household = FirmId.From(Guid.Parse(
      "b2000000-0000-4000-8000-000000000002"));
    var product = ProductId.From(Guid.Parse(
      "b3000000-0000-4000-8000-000000000001"));
    var category = ProductCategoryId.From(Guid.Parse(
      "b4000000-0000-4000-8000-000000000001"));
    var process = ProductionProcessId.From(Guid.Parse(
      "b5000000-0000-4000-8000-000000000001"));
    var facility = FacilityId.From(Guid.Parse(
      "b6000000-0000-4000-8000-000000000001"));
    var location = InventoryLocationId.From(Guid.Parse(
      "b7000000-0000-4000-8000-000000000001"));
    var unit = OperatingUnitId.From(Guid.Parse(
      "b8000000-0000-4000-8000-000000000001"));
    var activity = ActivityId.From(Guid.Parse(
      "ba000000-0000-4000-8000-000000000001"));

    var builder = new EconomyWorldBuilder(
      new EconomyPolicy
      {
        PeriodHours = SimulationHour.HoursPerDay,
        CohortBudgetResetMode = CohortBudgetResetMode.CarryForward,
        UseRegionLaborPools = true
      },
      RegistrationMode.Strict,
      EconomyModelSpecification.Default with
      {
        Version = "deterministic-bounded-1"
      });
    builder
      .AddProduct(new ProductDefinition(
        product,
        category,
        ImmutableArray<ProductInput>.Empty,
        ImmutableArray<ProductAttributeDefinition>.Empty,
        process,
        null))
      .AddRegion(region, 100, 2)
      .AddFirm(firm, "Bounded Producer", Money.From(500m))
      .AddFacility(new FacilityBinding(
        facility,
        firm,
        location,
        location,
        new FacilityLayout(
          ImmutableDictionary<OperatingUnitId, OperatingUnit>.Empty
            .Add(unit, new OperatingUnit(
              unit,
              OperatingUnitKind.Manufacturing,
              Quantity.From(10m))),
          ImmutableArray<MaterialRoute>.Empty),
        region))
      .AddCohort(new ConsumerCohort(
        ConsumerCohortId.From(Guid.Parse(
          "b9000000-0000-4000-8000-000000000001")),
        new PopulationCount(10),
        Money.From(100m),
        new PreferenceProfile(
          ImmutableArray.Create(new CategoryPreference(category, 1m)),
          PriceSensitivity: 0m,
          QualitySensitivity: 0m,
          BrandLoyalty: 0m),
        region,
        HouseholdProductivityKind.Mean,
        household))
      .AddInventory(
        firm,
        location,
        new ProductBatch(
          product,
          Quantity.From(20m),
          new ProductQuality(100m),
          Money.From(2m),
          SimulationDate.Epoch,
          null));

    var world = builder.Build();
    var cohorts = new Dictionary<CohortId, HouseholdCohort>(world.CoreState.Cohorts)
    {
      [CohortId.From(Guid.Parse(
        "b9000000-0000-4000-8000-000000000001"))] = new HouseholdCohort(
          CohortId.From(Guid.Parse(
            "b9000000-0000-4000-8000-000000000001")),
          region.AsCore(),
          10,
          new HouseholdProfile(0m, 0m, 1m, 0m),
          HouseholdLaborKind.Mean,
          Money.From(100m),
          household.AsCore())
    };
    var activities = new Dictionary<ActivityId, Activity>(world.CoreState.Activities)
    {
      [activity] = new Activity(
        activity,
        firm.AsCore(),
        region.AsCore(),
        new ActivityRecipe(
          ImmutableArray<ResourceAmount>.Empty,
          [new ResourceAmount(product.AsCore(), 1m)],
          LaborHoursPerRun: 1m,
          ProductionSpacePerRun: 1m),
        InstalledCapacity: 2m)
    };
    world.CoreState = world.CoreState with
    {
      Activities = activities,
      Cohorts = cohorts
    };
    return world;
  }
}
