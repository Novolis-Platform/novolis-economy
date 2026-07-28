using System.Collections.Immutable;
using Novolis.Economy;
using Novolis.Economy.Accounting;
using Novolis.Economy.Finance;
using Novolis.Economy.Production;
using Novolis.Economy.Simulation;

namespace Novolis.Economy.Unit.Scenarios;

public sealed class OwnershipAndDefaultTests
{
  private static (FirmId Issuer, FirmId Owner, FirmId Other, EconomySimulation Sim) TwoFirmWorld()
  {
    var issuer = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000b1"));
    var owner = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000b2"));
    var other = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000b3"));
    var builder = new EconomyWorldBuilder(new EconomyPolicy
    {
      HouseholdCreditFromWages = true,
      CohortBudgetResetMode = CohortBudgetResetMode.CarryForward,
    });
    builder.AddFirm(issuer, "Issuer", Money.From(5_000m));
    builder.AddCivic(owner, "CivicOwner", Money.From(1_000m), "test-registry");
    builder.AddFirm(other, "Other", Money.From(100m));
    return (issuer, owner, other, new EconomySimulation(11, builder.Build()));
  }

  [Test]
  public async Task AssignOwnership_And_Dividend_ConservesLiquid()
  {
    var (issuer, owner, _, sim) = TwoFirmWorld();
    var open = MoneyStock.Liquid(sim.State.World);

    sim.Enqueue(new AssignOwnership(issuer, owner, 1m));
    await sim.AdvanceAsync(SimulationDuration.FromHours(1));
    await Assert.That(sim.State.World.OwnershipClaims.Count).IsEqualTo(1);
    await Assert.That(sim.State.Events.OfType<OwnershipChanged>().Count()).IsEqualTo(1);

    sim.Enqueue(new DeclareDividend(issuer, Money.From(400m)));
    await sim.AdvanceAsync(SimulationDuration.FromHours(1));
    await Assert.That(sim.State.Events.OfType<DividendPaid>().Count()).IsEqualTo(1);
    await Assert.That(sim.State.World.Ledgers[owner].Cash.Amount).IsEqualTo(1_400m);
    await Assert.That(sim.State.World.Ledgers[issuer].Cash.Amount).IsEqualTo(4_600m);
    await Assert.That(MoneyStock.Liquid(sim.State.World)).IsEqualTo(open);
  }

  [Test]
  public async Task UpgradeFacility_RaisesCapacity_OrFailsOnCash()
  {
    var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c1"));
    var facilityId = FacilityId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c2"));
    var unitId = OperatingUnitId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c3"));
    var loc = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c4"));
    var layout = new FacilityLayout(
      ImmutableDictionary<OperatingUnitId, OperatingUnit>.Empty.Add(
        unitId, new OperatingUnit(unitId, OperatingUnitKind.Manufacturing, Quantity.From(10m))),
      ImmutableArray<MaterialRoute>.Empty);
    var builder = new EconomyWorldBuilder();
    builder.AddFirm(firm, "Plant", Money.From(200m));
    builder.AddFacility(new FacilityBinding(facilityId, firm, loc, null, layout));
    var sim = new EconomySimulation(13, builder.Build());
    await Assert.That(sim.State.World.Facilities[facilityId].ManufacturingCapacity.Value).IsEqualTo(10m);

    sim.Enqueue(new UpgradeFacility(facilityId, Money.From(50m), 1.25m));
    await sim.AdvanceAsync(SimulationDuration.FromHours(1));
    await Assert.That(sim.State.Events.OfType<FacilityUpgraded>().Count()).IsEqualTo(1);
    await Assert.That(sim.State.World.Facilities[facilityId].ManufacturingCapacity.Value).IsEqualTo(12.5m);
    await Assert.That(sim.State.World.Ledgers[firm].Cash.Amount).IsEqualTo(150m);

    sim.Enqueue(new UpgradeFacility(facilityId, Money.From(10_000m), 1.25m));
    await sim.AdvanceAsync(SimulationDuration.FromHours(1));
    await Assert.That(sim.State.Events.OfType<FacilityUpgradeFailed>().Count()).IsEqualTo(1);
    await Assert.That(sim.State.World.Facilities[facilityId].ManufacturingCapacity.Value).IsEqualTo(12.5m);
  }

  [Test]
  public async Task Default_Freezes_AbsorbsFacility_AndBlocksBorrow()
  {
    var lender = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d1"));
    var borrower = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d2"));
    var facilityId = FacilityId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d3"));
    var unitId = OperatingUnitId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d4"));
    var loc = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d5"));
    var layout = new FacilityLayout(
      ImmutableDictionary<OperatingUnitId, OperatingUnit>.Empty.Add(
        unitId, new OperatingUnit(unitId, OperatingUnitKind.Manufacturing, Quantity.From(4m))),
      ImmutableArray<MaterialRoute>.Empty);

    var builder = new EconomyWorldBuilder();
    builder.AddCivic(lender, "CivicLender", Money.From(5_000m), "civic");
    builder.AddFirm(borrower, "Borrower", Money.From(0m));
    builder.AddFacility(new FacilityBinding(facilityId, borrower, loc, null, layout));
    builder.SetOwnership(borrower, borrower, 1m);
    var sim = new EconomySimulation(17, builder.Build());

    sim.Enqueue(new OriginateLoan(lender, borrower, Money.From(500m), 0.1m, TermHours: 2));
    await sim.AdvanceAsync(SimulationDuration.FromHours(1));
    var b = sim.State.World.Ledgers[borrower];
    b.Post(AccountRole.WageExpense, AccountRole.Cash, Money.From(500m), sim.State.Clock.Date, "burn");
    await sim.AdvanceAsync(SimulationDuration.FromHours(2));

    await Assert.That(sim.State.Events.OfType<LoanDefaulted>().Count()).IsEqualTo(1);
    await Assert.That(sim.State.World.IsCreditFrozen(borrower)).IsTrue();
    await Assert.That(sim.State.Events.OfType<CreditFrozenSet>().Count()).IsEqualTo(1);
    await Assert.That(sim.State.World.Facilities[facilityId].FirmId).IsEqualTo(lender);
    await Assert.That(sim.State.Events.OfType<FacilityAbsorbed>().Count()).IsEqualTo(1);
    await Assert.That(
      sim.State.World.OwnershipClaims.Single(c => c.IssuerFirmId.Equals(borrower)).OwnerFirmId)
      .IsEqualTo(lender);

    // Top up lender and try to lend again — blocked by freeze.
    sim.State.World.Ledgers[lender].SeedCash(Money.From(1_000m), sim.State.Clock.Date);
    sim.Enqueue(new OriginateLoan(lender, borrower, Money.From(100m), 0.1m, TermHours: 10));
    await sim.AdvanceAsync(SimulationDuration.FromHours(1));
    await Assert.That(sim.State.World.Loans.Count(l => l.Status == LoanStatus.Active)).IsEqualTo(0);
  }

  [Test]
  public async Task EnsureFirm_CreatesFirmEntity_AddCivic_SetsKind()
  {
    var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000e1"));
    var civic = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000e2"));
    var builder = new EconomyWorldBuilder();
    builder.AddFirm(firm, "F", Money.From(1m));
    builder.AddCivic(civic, "C", Money.From(1m), "reg-1");
    var world = builder.Build();
    await Assert.That(world.Entities[firm].Kind).IsEqualTo(LegalEntityKind.Firm);
    await Assert.That(world.Entities[civic].Kind).IsEqualTo(LegalEntityKind.Civic);
    await Assert.That(world.Entities[civic].RegistryId).IsEqualTo("reg-1");
  }
}
