using System.Collections.Immutable;
using Novolis.Economy;
using Novolis.Economy.Agents;
using Novolis.Economy.Finance;
using Novolis.Economy.Population;
using Novolis.Economy.Production;
using Novolis.Economy.Simulation;
using Novolis.Economy.Simulation.Phases;

namespace Novolis.Economy.Unit.Scenarios;

public sealed class HouseholdRegionLaborTests
{
  private static PreferenceProfile Prefs() =>
    new(
      ImmutableArray<CategoryPreference>.Empty,
      PriceSensitivity: 1m,
      QualitySensitivity: 1m,
      BrandLoyalty: 0m);

  private static FacilityLayout MfgLayout(decimal capacity = 10m)
  {
    var unitId = OperatingUnitId.From(Guid.Parse("00000000-0000-4000-8000-0000000000f1"));
    return new FacilityLayout(
      ImmutableDictionary<OperatingUnitId, OperatingUnit>.Empty.Add(
        unitId, new OperatingUnit(unitId, OperatingUnitKind.Manufacturing, Quantity.From(capacity))),
      ImmutableArray<MaterialRoute>.Empty);
  }

  private static FacilityLayout RetailLayout()
  {
    var unitId = OperatingUnitId.From(Guid.Parse("00000000-0000-4000-8000-0000000000f2"));
    return new FacilityLayout(
      ImmutableDictionary<OperatingUnitId, OperatingUnit>.Empty.Add(
        unitId, new OperatingUnit(unitId, OperatingUnitKind.Sales, Quantity.From(100m))),
      ImmutableArray<MaterialRoute>.Empty);
  }

  [Test]
  public async Task RegionLaborPool_CapsFirmAvailability()
  {
    var area = GeographicAreaId.From(Guid.Parse("aaaaaaaa-0000-4000-8000-000000000001"));
    var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a1"));
    var facilityId = FacilityId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a2"));
    var loc = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a3"));
    var product = ProductId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a4"));
    var hh = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a5"));

    var builder = new EconomyWorldBuilder(new EconomyPolicy
    {
      PeoplePerHousehold = 4,
      LaborHoursPerOutputUnit = 1m,
      UseRegionLaborPools = true,
    });
    builder.AddRegion(area, livingCapacityHouseholds: 10, productionSlots: 4);
    builder.AddFirm(firm, "Plant", Money.From(1_000m));
    builder.AddFacility(new FacilityBinding(facilityId, firm, loc, null, MfgLayout(), area));
    builder.AddCohort(new ConsumerCohort(
      ConsumerCohortId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a6")),
      new PopulationCount(4),
      Money.From(500m),
      Prefs(),
      area,
      HouseholdProductivityKind.Mean,
      hh));
    builder.SetLabor(firm, 999m); // ignored when region-only facilities
    var world = builder.Build();
    world.ProductionPlans[(firm, facilityId, product)] = Quantity.From(100m);

    // Mean = 18 hh-hours/day → 18/24 = 0.75 per tick for 1 household.
    AllocateLaborPhase.ApplyRegionLaborPools(world);
    await Assert.That(world.AvailableLaborHours[firm]).IsEqualTo(0.75m);
  }

  [Test]
  public async Task Comfort_BlocksHouseholdLendAndInvest()
  {
    var area = GeographicAreaId.From(Guid.Parse("aaaaaaaa-0000-4000-8000-000000000011"));
    var hh = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000b1"));
    var borrower = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000b2"));
    var issuer = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000b3"));

    var builder = new EconomyWorldBuilder(new EconomyPolicy
    {
      PeoplePerHousehold = 4,
      HouseholdComfortThresholdPerHousehold = Money.From(50m),
      CohortBudgetResetMode = CohortBudgetResetMode.CarryForward,
    });
    builder.AddRegion(area, 10, 4);
    builder.AddFirm(borrower, "Borrower", Money.From(10m));
    builder.AddFirm(issuer, "Issuer", Money.From(100m));
    builder.AddCohort(new ConsumerCohort(
      ConsumerCohortId.From(Guid.Parse("00000000-0000-4000-8000-0000000000b4")),
      new PopulationCount(4),
      Money.From(50m), // exactly at comfort floor (50 × 1 hh)
      Prefs(),
      area,
      HouseholdFirmId: hh));
    var sim = new EconomySimulation(21, builder.Build());
    var cohort = sim.State.World.Cohorts[0];
    await Assert.That(sim.State.World.IsAboveComfort(cohort)).IsFalse();

    sim.Enqueue(new OriginateLoan(hh, borrower, Money.From(10m), 0.1m, 24));
    sim.Enqueue(new PurchaseOwnership(issuer, hh, 0.1m, Money.From(10m)));
    await sim.AdvanceAsync(SimulationDuration.FromHours(1));

    await Assert.That(sim.State.World.Loans.Count).IsEqualTo(0);
    await Assert.That(sim.State.Events.OfType<OwnershipPurchased>().Count()).IsEqualTo(0);
    await Assert.That(cohort.BudgetRemaining.Amount).IsEqualTo(50m);
  }

  [Test]
  public async Task PurchaseOwnership_MovesBudgetToIssuer_AndClaim()
  {
    var area = GeographicAreaId.From(Guid.Parse("aaaaaaaa-0000-4000-8000-000000000021"));
    var hh = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c1"));
    var issuer = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c2"));

    var builder = new EconomyWorldBuilder(new EconomyPolicy
    {
      PeoplePerHousehold = 4,
      HouseholdComfortThresholdPerHousehold = Money.From(50m),
      CohortBudgetResetMode = CohortBudgetResetMode.CarryForward,
    });
    builder.AddRegion(area, 10, 4);
    builder.AddFirm(issuer, "Issuer", Money.From(100m));
    builder.AddCohort(new ConsumerCohort(
      ConsumerCohortId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c3")),
      new PopulationCount(4),
      Money.From(200m),
      Prefs(),
      area,
      HouseholdFirmId: hh));
    var sim = new EconomySimulation(22, builder.Build());
    var open = MoneyStock.Liquid(sim.State.World);

    sim.Enqueue(new PurchaseOwnership(issuer, hh, 0.25m, Money.From(40m)));
    await sim.AdvanceAsync(SimulationDuration.FromHours(1));

    await Assert.That(sim.State.Events.OfType<OwnershipPurchased>().Count()).IsEqualTo(1);
    await Assert.That(sim.State.World.Cohorts[0].BudgetRemaining.Amount).IsEqualTo(160m);
    await Assert.That(sim.State.World.Ledgers[issuer].Cash.Amount).IsEqualTo(140m);
    await Assert.That(sim.State.World.OwnershipClaims.Single().Fraction).IsEqualTo(0.25m);
    await Assert.That(MoneyStock.Liquid(sim.State.World)).IsEqualTo(open);
  }

  [Test]
  public async Task LivingCapacity_ClampsCohortPopulation()
  {
    var area = GeographicAreaId.From(Guid.Parse("aaaaaaaa-0000-4000-8000-000000000031"));
    var builder = new EconomyWorldBuilder(new EconomyPolicy { PeoplePerHousehold = 4 });
    builder.AddRegion(area, livingCapacityHouseholds: 1, productionSlots: 2);
    builder.AddCohort(new ConsumerCohort(
      ConsumerCohortId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d1")),
      new PopulationCount(40),
      Money.From(100m),
      Prefs(),
      area));
    var world = builder.Build();
    await Assert.That(world.Cohorts.Count).IsEqualTo(1);
    await Assert.That(world.Cohorts[0].Definition.Population.Value).IsEqualTo(4);
    await Assert.That(world.UsedLivingHouseholds(area)).IsEqualTo(1);
  }

  [Test]
  public async Task ProductionSlot_IgnoresRetail_EnforcesMfg()
  {
    var area = GeographicAreaId.From(Guid.Parse("aaaaaaaa-0000-4000-8000-000000000041"));
    var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000e1"));
    var mfg1 = FacilityId.From(Guid.Parse("00000000-0000-4000-8000-0000000000e2"));
    var mfg2 = FacilityId.From(Guid.Parse("00000000-0000-4000-8000-0000000000e3"));
    var retail = FacilityId.From(Guid.Parse("00000000-0000-4000-8000-0000000000e4"));
    var loc1 = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-0000000000e5"));
    var loc2 = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-0000000000e6"));
    var loc3 = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-0000000000e7"));

    var builder = new EconomyWorldBuilder();
    builder.AddRegion(area, 10, productionSlots: 1);
    builder.AddFirm(firm, "F", Money.From(1m));
    builder.AddFacility(new FacilityBinding(mfg1, firm, loc1, null, MfgLayout(), area));
    builder.AddFacility(new FacilityBinding(retail, firm, loc2, loc2, RetailLayout(), area));
    builder.AddFacility(new FacilityBinding(mfg2, firm, loc3, null, MfgLayout(), area));
    var world = builder.Build();

    await Assert.That(world.Facilities.ContainsKey(mfg1)).IsTrue();
    await Assert.That(world.Facilities.ContainsKey(retail)).IsTrue();
    await Assert.That(world.Facilities.ContainsKey(mfg2)).IsFalse();
    await Assert.That(world.UsedProductionSlots(area)).IsEqualTo(1);
  }

  [Test]
  public async Task Wages_CreditInFacilityAreaOnly()
  {
    var areaA = GeographicAreaId.From(Guid.Parse("aaaaaaaa-0000-4000-8000-000000000051"));
    var areaB = GeographicAreaId.From(Guid.Parse("aaaaaaaa-0000-4000-8000-000000000052"));
    var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000f1"));
    var facilityId = FacilityId.From(Guid.Parse("00000000-0000-4000-8000-0000000000f3"));
    var loc = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-0000000000f4"));

    var builder = new EconomyWorldBuilder(new EconomyPolicy { HouseholdCreditFromWages = true });
    builder.AddRegion(areaA, 10, 4);
    builder.AddRegion(areaB, 10, 4);
    builder.AddFirm(firm, "Plant", Money.From(1_000m));
    builder.AddFacility(new FacilityBinding(facilityId, firm, loc, null, MfgLayout(), areaA));
    builder.AddCohort(new ConsumerCohort(
      ConsumerCohortId.From(Guid.Parse("00000000-0000-4000-8000-0000000000f5")),
      new PopulationCount(100),
      Money.From(0m),
      Prefs(),
      areaA));
    builder.AddCohort(new ConsumerCohort(
      ConsumerCohortId.From(Guid.Parse("00000000-0000-4000-8000-0000000000f6")),
      new PopulationCount(100),
      Money.From(0m),
      Prefs(),
      areaB));
    var world = builder.Build();

    SettleInvoicesAndWagesPhase.DistributeWageCreditsForFirm(world, firm, 100m);
    var a = world.Cohorts.Single(c => c.Definition.Area.Equals(areaA));
    var b = world.Cohorts.Single(c => c.Definition.Area.Equals(areaB));
    await Assert.That(a.BudgetRemaining.Amount).IsEqualTo(100m);
    await Assert.That(b.BudgetRemaining.Amount).IsEqualTo(0m);
  }

  [Test]
  public async Task HouseholdFirmAgent_ComfortHold_ThenInvest()
  {
    var area = GeographicAreaId.From(Guid.Parse("aaaaaaaa-0000-4000-8000-000000000061"));
    var hh = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000061"));
    var issuer = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000062"));

    var builder = new EconomyWorldBuilder(new EconomyPolicy
    {
      PeoplePerHousehold = 4,
      HouseholdComfortThresholdPerHousehold = Money.From(50m),
      CohortBudgetResetMode = CohortBudgetResetMode.CarryForward,
    });
    builder.AddRegion(area, 10, 4);
    builder.AddFirm(issuer, "Issuer", Money.From(100m));
    builder.AddCohort(new ConsumerCohort(
      ConsumerCohortId.From(Guid.Parse("00000000-0000-4000-8000-000000000063")),
      new PopulationCount(4),
      Money.From(50m),
      Prefs(),
      area,
      HouseholdFirmId: hh));
    var sim = new EconomySimulation(31, builder.Build());
    var agent = new HouseholdFirmAgent(hh, new HouseholdFirmAgentPolicy(
      PreferredIssuer: issuer,
      PurchaseFraction: 0.05m,
      PurchasePrice: Money.From(10m)));
    var ctx = new AgentContext(sim, new DeterministicRandom(1));

    agent.Tick(ctx);
    await Assert.That(agent.LastDecision).IsEqualTo("comfort hold");

    sim.State.World.Cohorts[0].BudgetRemaining = Money.From(120m);
    agent.Tick(ctx);
    await Assert.That(agent.LastDecision).IsEqualTo("invest 10");
    await sim.AdvanceAsync(SimulationDuration.FromHours(1));
    await Assert.That(sim.State.Events.OfType<OwnershipPurchased>().Count()).IsEqualTo(1);
  }
}
