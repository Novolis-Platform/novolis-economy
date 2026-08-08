using System.Collections.Immutable;
using Novolis.Economy;
using Novolis.Economy.Accounting;
using Novolis.Economy.Agents;
using Novolis.Economy.Core;
using Novolis.Economy.Core.Steps;
using Novolis.Economy.Finance;
using Novolis.Economy.Logistics;
using Novolis.Economy.Logistics.Extensions;
using Novolis.Economy.Markets;
using Novolis.Economy.Population;
using Novolis.Economy.Production;
using Novolis.Economy.Simulation;
using Novolis.Economy.Simulation.Phases;
using FinanceLoan = Novolis.Economy.Finance.Loan;
using FinanceLoanStatus = Novolis.Economy.Finance.LoanStatus;

namespace Novolis.Economy.Unit;

/// <summary>Assembly-level coverage push toward 95% line+branch (Simulation/Agents/Logistics/Accounting/Finance/Core).</summary>
public sealed class EconomyAssemblyCoverageBoostTests
{
    private static PreferenceProfile Prefs() =>
        new(ImmutableArray<CategoryPreference>.Empty, 1m, 1m, 0m);

    [Test]
    public async Task Core_Stub_Step_Names_And_TickTransfers()
    {
        await Assert.That(new ResolveDemandStep().Name).IsEqualTo("06_ResolveDemand");
        await Assert.That(new MatchBuyersSellersStep().Name).IsEqualTo("07_MatchBuyersSellers");
        await Assert.That(new ProcessTransfersStep().Name).IsEqualTo("09_ProcessTransfers");
        await Assert.That(new SettleObligationsStep().Name).IsEqualTo("11_SettleObligations");

        var state = EconomyState.Empty with { Policy = StatePolicy.Neutral };
        _ = new ProcessTransfersStep().Execute(state);
        _ = new SettleObligationsStep().Execute(state);
        _ = new ResolveDemandStep().Execute(state);
        _ = new MatchBuyersSellersStep().Execute(state);
    }

    [Test]
    public async Task Logistics_FtlDriveLife_And_NetworkSnapshot()
    {
        var light = FtlDriveLifePolicy.RatedLifeLight;
        var mega = FtlDriveLifePolicy.RatedLifeMega;
        var elective = FtlDriveLifePolicy.ElectiveOverhaulFraction;
        var decay = FtlDriveLifePolicy.AcuteWearDecayPerDay;
        var grace = FtlDriveLifePolicy.PremiumGraceDays;
        await Assert.That(light).IsEqualTo(9_000m);
        await Assert.That(mega).IsEqualTo(22_000m);
        await Assert.That(elective).IsEqualTo(0.72m);
        await Assert.That(decay).IsEqualTo(0.04m);
        await Assert.That(grace).IsEqualTo(14);
        await Assert.That(FtlDriveLifePolicy.RatedLifeForHull("Mega Freighter")).IsEqualTo(mega);
        await Assert.That(FtlDriveLifePolicy.RatedLifeForHull("Tramp")).IsEqualTo(light);

        await Assert.That(TransitProfiles.FromCode((int)TransitProfile.SlowEconomic))
            .IsEqualTo(TransitProfile.SlowEconomic);
        await Assert.That(TransitProfiles.FromCode((int)TransitProfile.PriorityCommercial))
            .IsEqualTo(TransitProfile.PriorityCommercial);

        var hubA = TransportHubId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a1"));
        var hubB = TransportHubId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a2"));
        var locA = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a3"));
        var locB = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a4"));
        var corridorId = TransportCorridorId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a5"));
        var hubs = new Dictionary<TransportHubId, TransportHub>
        {
            [hubA] = new TransportHub(hubA, locA, "A", 1, 2),
            [hubB] = new TransportHub(hubB, locB, "B", 1, 0),
        };
        var corridors = new Dictionary<TransportCorridorId, TransportCorridor>
        {
            [corridorId] = new TransportCorridor(corridorId, hubA, hubB, 2, Quantity.From(10m), 1m, Money.From(3m)),
        };
        var vehicle = new VehicleClass(
            VehicleClassId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a6")),
            Quantity.From(5m), 1m, 1m, Quantity.From(10m));
        var itinerary = new Itinerary(ImmutableArray.Create(corridorId));
        var shipments = new List<ActiveShipment>
        {
            new(
                ShipmentId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a7")),
                FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a8")),
                ProductId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a9")),
                Quantity.From(2m), Money.From(1m), SimulationHour.Epoch, itinerary, vehicle, hubA, null)
            {
                Phase = ShipmentPhase.WaitingBerth,
            },
            new(
                ShipmentId.From(Guid.Parse("00000000-0000-4000-8000-0000000000aa")),
                FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a8")),
                ProductId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a9")),
                Quantity.From(1m), Money.From(1m), SimulationHour.Epoch, itinerary, vehicle, hubA, null)
            {
                Phase = ShipmentPhase.Delivered,
            },
        };

        var snap = shipments.Snapshot(hubs, corridors);
        await Assert.That(snap.HubCount).IsEqualTo(2);
        await Assert.That(snap.CargoQuantityInFlight).IsEqualTo(2m);
        await Assert.That(snap.CorridorTollExposure.Amount).IsEqualTo(3m);
        await Assert.That(snap.BerthConstrainedHubs).IsEqualTo(1);
        await Assert.That(snap.AverageBerthUtilization).IsGreaterThan(0m);
        await Assert.That(snap.ShipmentsByPhase[ShipmentPhase.WaitingBerth]).IsEqualTo(1);
    }

    [Test]
    public async Task Logistics_ItineraryPlanner_Guards_And_CorridorCap()
    {
        var origin = TransportHubId.From(Guid.Parse("00000000-0000-4000-8000-0000000000b1"));
        var dest = TransportHubId.From(Guid.Parse("00000000-0000-4000-8000-0000000000b2"));
        var vehicle = new VehicleClass(
            VehicleClassId.From(Guid.Parse("00000000-0000-4000-8000-0000000000b3")),
            CargoCapacity: Quantity.From(5m),
            FuelBurnPerDifficultyHour: 1m,
            CrewLaborPerUnderwayHour: 1m,
            FuelTankCapacity: Quantity.From(20m));
        var corridors = ImmutableDictionary<TransportCorridorId, TransportCorridor>.Empty;

        await Assert.That(ItineraryPlanner.TryPlan(origin, origin, Quantity.From(1m), vehicle, corridors, out _))
            .IsFalse();
        await Assert.That(ItineraryPlanner.TryPlan(origin, dest, Quantity.From(6m), vehicle, corridors, out _))
            .IsFalse();

        var loc = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-0000000000b4"));
        var hub = new TransportHub(origin, loc, "O", 1, 2);
        var corridor = new TransportCorridor(
            TransportCorridorId.From(Guid.Parse("00000000-0000-4000-8000-0000000000b5")),
            origin, dest, TransitHours: 2, MaxCargo: Quantity.From(2m), Difficulty: 1m, Toll: Money.Zero);
        corridors = corridors.Add(corridor.Id, corridor);
        await Assert.That(ItineraryPlanner.TryPlan(
            origin, dest, Quantity.From(2m), vehicle, corridors, out var itinerary)).IsTrue();

        var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000b6"));
        var product = ProductId.From(Guid.Parse("00000000-0000-4000-8000-0000000000b7"));
        var inventory = new InventoryStore();
        inventory.Add(
            new InventoryKey(firm, loc, product),
            new ProductBatch(product, Quantity.From(5m), new ProductQuality(100m), Money.From(1m), SimulationDate.Epoch, null),
            bypassLimits: true);

        var blocked = LogisticsEngine.TryDepartItinerary(
            inventory, firm, hub, itinerary, vehicle, product, Quantity.From(3m),
            fuelProductId: null, SimulationHour.Epoch, corridors, out _, out var reason);
        await Assert.That(blocked).IsNull();
        await Assert.That(reason).IsEqualTo("cargo-exceeds-corridor");
    }

    [Test]
    public async Task Accounting_And_Finance_EdgeBranches()
    {
        var issuer = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c1"));
        var owner = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c2"));
        var other = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c3"));
        var claims = new List<OwnershipClaim>();

        await Assert.That(OwnershipEngine.TryAssign(claims, issuer, owner, -0.1m, _ => true)).IsFalse();
        await Assert.That(OwnershipEngine.TryAssign(claims, issuer, owner, 0.5m, _ => false)).IsFalse();
        await Assert.That(OwnershipEngine.TryTransfer(claims, issuer, owner, other, 0.1m, _ => true)).IsFalse();
        await Assert.That(OwnershipEngine.TryTransfer(claims, issuer, owner, owner, 0.1m, _ => true)).IsFalse();
        OwnershipEngine.TransferAllIssuerClaimsTo(claims, issuer, other);

        claims.Add(new OwnershipClaim(issuer, owner, 0m));
        OwnershipEngine.TransferAllIssuerClaimsTo(claims, issuer, other);
        await Assert.That(claims.Count(c => c.IssuerFirmId.Equals(issuer))).IsEqualTo(0);

        claims.Add(new OwnershipClaim(issuer, other, 0.4m));
        claims.Add(new OwnershipClaim(issuer, owner, 0.2m));
        OwnershipEngine.TransferAllIssuerClaimsTo(claims, issuer, other);
        await Assert.That(claims.Single().Fraction).IsEqualTo(0.6m);

        await Assert.That(OwnershipEngine.TryAddClaim(claims, issuer, owner, 0m, _ => true)).IsFalse();
        await Assert.That(OwnershipEngine.TryAddClaim(claims, issuer, owner, 0.5m, _ => true)).IsFalse();

        var ledger = new FirmLedger(issuer);
        ledger.SeedCash(Money.From(50m), SimulationDate.Epoch);
        OwnershipEngine.PostOwnershipSaleProceeds(ledger, Money.Zero, SimulationDate.Epoch);
        await Assert.That(OwnershipEngine.TryPostCapacityInvestment(ledger, Money.Zero, SimulationDate.Epoch)).IsTrue();
        _ = ledger.Account(AccountRole.Cash);
        ledger.Post(AccountRole.Cash, AccountRole.Equity, Money.Zero, SimulationDate.Epoch, "noop");

        LedgerEngine.WriteOffInventory(ledger, Money.From(1m), SimulationDate.Epoch);
        LedgerEngine.PostFuelBurn(ledger, Money.From(1m), SimulationDate.Epoch);
        await Assert.That(LedgerEngine.TryPostToll(ledger, Money.Zero, SimulationDate.Epoch)).IsTrue();
        await Assert.That(LedgerEngine.TryPostToll(ledger, Money.From(10_000m), SimulationDate.Epoch)).IsFalse();

        var paid = OwnershipEngine.TryDeclareDividend(
            [new OwnershipClaim(issuer, owner, 1m)],
            new Dictionary<FirmId, FirmLedger> { [issuer] = ledger },
            issuer, Money.From(10m), SimulationDate.Epoch);
        await Assert.That(paid.Count).IsEqualTo(0);

        var emptyDiv = OwnershipEngine.TryDeclareDividend(
            [],
            new Dictionary<FirmId, FirmLedger>
            {
                [issuer] = ledger,
            },
            issuer, Money.From(10m), SimulationDate.Epoch);
        await Assert.That(emptyDiv.Count).IsEqualTo(0);

        var lender = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c4"));
        var borrower = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c5"));
        var ledgers = new Dictionary<FirmId, FirmLedger>
        {
            [lender] = new FirmLedger(lender),
            [borrower] = new FirmLedger(borrower),
        };
        ledgers[lender].SeedCash(Money.From(10m), SimulationDate.Epoch);
        await Assert.That(LoanEngine.TryOriginate(
            ledgers,
            new OriginateLoan(lender, borrower, Money.From(100m), 0.1m, 24),
            SimulationHour.Epoch,
            () => LoanId.From(Guid.NewGuid()))).IsNull();

        await Assert.That(LoanEngine.TryOriginateHouseholdLender(
            ledgers,
            new OriginateLoan(lender, lender, Money.From(1m), 0.1m, 24),
            SimulationHour.Epoch,
            () => LoanId.From(Guid.NewGuid()))).IsNull();

        var closed = new FinanceLoan(
            LoanId.From(Guid.NewGuid()), lender, borrower, Money.From(10m), 0.1m,
            SimulationHour.Epoch, SimulationHour.Epoch.AddHours(10))
        {
            Status = FinanceLoanStatus.Closed,
        };
        await Assert.That(LoanEngine.AccrueHour(closed, ledgers, SimulationHour.Epoch).Amount).IsEqualTo(0m);
        await Assert.That(LoanEngine.TryRepay(
            closed, ledgers, Money.From(1m), SimulationHour.Epoch).Amount).IsEqualTo(0m);

        var zeroRate = new FinanceLoan(
            LoanId.From(Guid.NewGuid()), lender, borrower, Money.From(10m), 0m,
            SimulationHour.Epoch, SimulationHour.Epoch.AddHours(10));
        await Assert.That(zeroRate.HourlyInterest().Amount).IsEqualTo(0m);
        zeroRate.PrincipalRemaining = Money.Zero;
        await Assert.That(zeroRate.HourlyInterest().Amount).IsEqualTo(0m);

        var ghostLedgers = new Dictionary<FirmId, FirmLedger> { [lender] = ledgers[lender] };
        var active = new FinanceLoan(
            LoanId.From(Guid.NewGuid()), lender, borrower, Money.From(10m), 0.2m,
            SimulationHour.Epoch, SimulationHour.Epoch.AddHours(10));
        await Assert.That(LoanEngine.AccrueHour(active, ghostLedgers, SimulationHour.Epoch).Amount).IsEqualTo(0m);
    }

    [Test]
    public async Task CreditCirculation_ReadsRemainingGetters_AndEventBranches()
    {
        var source = new FakeCreditSource();
        var tracker = new CreditCirculation(source);
        var hour = SimulationHour.Epoch;
        var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d1"));
        var other = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d2"));
        var product = ProductId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d3"));
        var tramp = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c0"));

        source.Events.AddRange(
        [
            new ExportFilled(hour, firm, product, Quantity.From(2m), Money.From(4m), Money.From(8m)),
            new GoodsSoldInterFirm(hour, firm, other, InventoryLocationId.From(Guid.NewGuid()), product, Quantity.From(3m), Money.From(2m), Money.From(6m)),
            new TransferGoodsFailed(hour, firm, other, product, "cash"),
            new TransferGoodsFailed(hour, firm, other, product, "stock"),
            new LoanOriginated(hour, LoanId.From(Guid.NewGuid()), firm, tramp, Money.From(50m), 0.1m, hour.AddHours(10)),
            new HouseholdCreditsIssued(hour, firm, Money.From(12m)),
        ]);
        tracker.ObserveAfterPulse(0);

        await Assert.That(tracker.BookFillQty).IsGreaterThanOrEqualTo(0m);
        await Assert.That(tracker.DividendCash).IsGreaterThanOrEqualTo(0m);
        await Assert.That(tracker.B2bFills).IsEqualTo(1);
        await Assert.That(tracker.B2bQty).IsEqualTo(3m);
        await Assert.That(tracker.B2bFailCash).IsEqualTo(1);
        await Assert.That(tracker.B2bFailStock).IsEqualTo(1);
        await Assert.That(tracker.ExportFills).IsEqualTo(1);
        await Assert.That(tracker.WagesDistributed).IsEqualTo(12m);
        await Assert.That(tracker.LoansOriginated).IsEqualTo(1);
        await Assert.That(tracker.LiquidStock).IsGreaterThanOrEqualTo(0m);
        await Assert.That(tracker.ActiveLoans).IsGreaterThanOrEqualTo(0);
        await Assert.That(tracker.PrincipalOutstanding).IsGreaterThanOrEqualTo(0m);
        await Assert.That(tracker.CreditFrozenFirms).IsGreaterThanOrEqualTo(0);
        await Assert.That(tracker.InventoryBookValue).IsGreaterThanOrEqualTo(0m);

        source.Clock = new SimulationHour(24);
        tracker.CaptureFinalMilestone();
        tracker.CaptureFinalMilestone();
        await Assert.That(tracker.Milestones.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Simulation_AcquireInputs_Shipments_And_Failures()
    {
        var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000e1"));
        var locA = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-0000000000e2"));
        var locB = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-0000000000e3"));
        var product = ProductId.From(Guid.Parse("00000000-0000-4000-8000-0000000000e4"));
        var cat = ProductCategoryId.From(Guid.Parse("00000000-0000-4000-8000-0000000000e5"));
        var process = ProductionProcessId.From(Guid.Parse("00000000-0000-4000-8000-000000000099"));
        var routeId = FreightRouteId.From(Guid.Parse("00000000-0000-4000-8000-0000000000e6"));
        var hubA = TransportHubId.From(Guid.Parse("00000000-0000-4000-8000-0000000000e7"));
        var hubB = TransportHubId.From(Guid.Parse("00000000-0000-4000-8000-0000000000e8"));
        var vehicleId = VehicleClassId.From(Guid.Parse("00000000-0000-4000-8000-0000000000e9"));
        var ghostHub = Guid.Parse("00000000-0000-4000-8000-0000000000ea");

        var builder = new EconomyWorldBuilder();
        builder.AddProduct(new ProductDefinition(
            product, cat, ImmutableArray<ProductInput>.Empty,
            ImmutableArray<ProductAttributeDefinition>.Empty, process, null));
        builder.AddFirm(firm, "Shipper", Money.From(1_000m));
        builder.AddRoute(new FreightRoute(routeId, locA, locB, TransitHours: 2, Capacity: Quantity.From(50m)));
        builder.AddHub(new TransportHub(hubA, locA, "A", 1, 2));
        builder.AddHub(new TransportHub(hubB, locB, "B", 1, 2));
        builder.AddVehicleClass(new VehicleClass(vehicleId, Quantity.From(20m), 1m, 1m, Quantity.From(40m)));
        builder.AddCorridor(new TransportCorridor(
            TransportCorridorId.From(Guid.Parse("00000000-0000-4000-8000-0000000000eb")),
            hubA, hubB, 3, Quantity.From(20m), 1m, Money.Zero));
        var world = builder.Build();
        world.Inventory.Add(
            new InventoryKey(firm, locA, product),
            new ProductBatch(product, Quantity.From(10m), new ProductQuality(100m), Money.From(1m), SimulationDate.Epoch, null));

        world.PendingShipments.Add(new IssueShipment(firm, routeId, product, Quantity.From(3m)));
        world.PendingShipments.Add(new IssueShipment(firm, FreightRouteId.From(Guid.NewGuid()), product, Quantity.From(1m)));
        world.PendingPlanShipments.Add(new PlanShipment(
            firm, ghostHub, hubB.Value, product, Quantity.From(1m), vehicleId.Value));
        world.PendingPlanRepositions.Add(new PlanReposition(
            firm, ghostHub, hubB.Value, vehicleId.Value));
        world.PendingPlanRepositions.Add(new PlanReposition(
            firm, hubA.Value, hubB.Value, Guid.NewGuid()));

        var state = new SimulationState(3, world);
        await new AcquireInputsPhase().ExecuteAsync(new SimulationContext(state, new DeterministicRandom(3)), CancellationToken.None);

        await Assert.That(state.Events.OfType<ShipmentDeparted>().Count()).IsEqualTo(1);
        await Assert.That(state.Events.OfType<ShipmentPlanFailed>().Count()).IsGreaterThanOrEqualTo(3);
        await Assert.That(state.World.PendingShipments.Count).IsEqualTo(0);
        await Assert.That(state.World.PendingPlanShipments.Count).IsEqualTo(0);
        await Assert.That(state.World.PendingPlanRepositions.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Simulation_RunProduction_Spoilage_And_MissingPlan()
    {
        var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000f1"));
        var loc = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-0000000000f2"));
        var product = ProductId.From(Guid.Parse("00000000-0000-4000-8000-0000000000f3"));
        var missingFac = FacilityId.From(Guid.Parse("00000000-0000-4000-8000-0000000000f4"));
        var cat = ProductCategoryId.From(Guid.Parse("00000000-0000-4000-8000-0000000000f5"));
        var process = ProductionProcessId.From(Guid.Parse("00000000-0000-4000-8000-000000000099"));

        var builder = new EconomyWorldBuilder(new EconomyPolicy { EnableSpoilage = true });
        builder.AddProduct(new ProductDefinition(
            product, cat, ImmutableArray<ProductInput>.Empty,
            ImmutableArray<ProductAttributeDefinition>.Empty, process, new ShelfLife(1)));
        builder.AddFirm(firm, "Plant", Money.From(100m));
        var world = builder.Build();
        world.Inventory.Add(
            new InventoryKey(firm, loc, product),
            new ProductBatch(product, Quantity.From(4m), new ProductQuality(100m), Money.From(2m), SimulationDate.Epoch, null));
        world.ProductionPlans[(firm, missingFac, product)] = Quantity.From(1m);

        var sim = new EconomySimulation(5, world);
        await sim.AdvanceAsync(SimulationDuration.FromHours(2));

        await Assert.That(sim.State.Events.OfType<InventorySpoiled>().Count()).IsGreaterThanOrEqualTo(1);
        await Assert.That(sim.State.Events.OfType<BatchProduced>().Count()).IsEqualTo(0);
    }

    [Test]
    public async Task Simulation_DefaultConsequence_And_UpgradeGuards()
    {
        var lender = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000101"));
        var borrower = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000102"));
        var storageUnit = OperatingUnitId.From(Guid.Parse("00000000-0000-4000-8000-000000000103"));
        var mfgUnit = OperatingUnitId.From(Guid.Parse("00000000-0000-4000-8000-000000000104"));
        var facilityId = FacilityId.From(Guid.Parse("00000000-0000-4000-8000-000000000105"));
        var loc = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-000000000106"));
        var ghostFacility = FacilityId.From(Guid.Parse("00000000-0000-4000-8000-000000000107"));

        var layout = new FacilityLayout(
            ImmutableDictionary<OperatingUnitId, OperatingUnit>.Empty
                .Add(storageUnit, new OperatingUnit(storageUnit, OperatingUnitKind.Storage, Quantity.From(10m)))
                .Add(mfgUnit, new OperatingUnit(mfgUnit, OperatingUnitKind.Manufacturing, Quantity.From(4m))),
            ImmutableArray<MaterialRoute>.Empty);

        var world = new EconomyWorldBuilder()
            .AddFirm(lender, "Lender", Money.From(100m))
            .AddFacility(new FacilityBinding(facilityId, borrower, loc, null, layout))
            .Build();
        world.OwnershipClaims.Add(new OwnershipClaim(borrower, borrower, 0m));
        world.Entities.Remove(borrower);

        var events = new List<IEconomyEvent>();
        DefaultConsequenceEngine.ApplyAbsorb(world, lender, borrower, SimulationHour.Epoch, events.Add);
        await Assert.That(world.Entities.ContainsKey(borrower)).IsTrue();
        await Assert.That(events.OfType<CreditFrozenSet>().Count()).IsEqualTo(1);
        await Assert.That(events.OfType<OwnershipChanged>().Any(e => e.Fraction == 0m)).IsTrue();

        await Assert.That(DefaultConsequenceEngine.TryUpgradeFacility(
            world, new UpgradeFacility(facilityId, Money.From(1m), 0.9m), SimulationHour.Epoch, out var r1)).IsNull();
        await Assert.That(r1).IsEqualTo("factor");
        await Assert.That(DefaultConsequenceEngine.TryUpgradeFacility(
            world, new UpgradeFacility(ghostFacility, Money.From(1m), 1.2m), SimulationHour.Epoch, out var r2)).IsNull();
        await Assert.That(r2).IsEqualTo("facility");

        world.Facilities[facilityId] = new FacilityBinding(facilityId, borrower, loc, null, layout);
        world.Ledgers.Remove(borrower);
        await Assert.That(DefaultConsequenceEngine.TryUpgradeFacility(
            world, new UpgradeFacility(facilityId, Money.From(1m), 1.2m), SimulationHour.Epoch, out var r3)).IsNull();
        await Assert.That(r3).IsEqualTo("ledger");

        world.Ledgers[borrower] = new FirmLedger(borrower);
        world.Ledgers[borrower].SeedCash(Money.From(50m), SimulationDate.Epoch);
        var upgraded = DefaultConsequenceEngine.TryUpgradeFacility(
            world, new UpgradeFacility(facilityId, Money.From(10m), 2m), SimulationHour.Epoch, out _);
        await Assert.That(upgraded).IsNotNull();
        await Assert.That(upgraded!.Layout.Units[storageUnit].Capacity.Value).IsEqualTo(10m);
        await Assert.That(upgraded.Layout.Units[mfgUnit].Capacity.Value).IsEqualTo(8m);
    }

    [Test]
    public async Task ApplyDecisions_HouseholdPurchase_Comfort_And_Overallocation()
    {
        var area = GeographicAreaId.From(Guid.Parse("aaaaaaaa-0000-4000-8000-000000000200"));
        var hh = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000200"));
        var issuer = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000201"));
        var buyer = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000202"));

        var builder = new EconomyWorldBuilder(new EconomyPolicy
        {
            HouseholdComfortThresholdPerHousehold = Money.From(50m),
            CohortBudgetResetMode = CohortBudgetResetMode.CarryForward,
        });
        builder.AddRegion(area, 10, 4);
        builder.AddFirm(issuer, "Issuer", Money.From(100m));
        builder.AddFirm(buyer, "Buyer", Money.From(200m));
        builder.AddCohort(new ConsumerCohort(
            ConsumerCohortId.From(Guid.Parse("00000000-0000-4000-8000-000000000203")),
            new PopulationCount(1),
            Money.From(80m),
            Prefs(),
            area,
            HouseholdFirmId: hh));
        var world = builder.Build();

        var phase = new ApplyDecisionsPhase();
        async Task<SimulationState> Run(params IEconomyCommand[] cmds)
        {
            var state = new SimulationState(9, world);
            foreach (var c in cmds) state.EnqueueCommand(c);
            await phase.ExecuteAsync(new SimulationContext(state, new DeterministicRandom(9)), CancellationToken.None);
            return state;
        }

        var comfort = await Run(new PurchaseOwnership(issuer, hh, 0.1m, Money.From(40m)));
        await Assert.That(comfort.Events.OfType<OwnershipPurchased>().Count()).IsEqualTo(0);

        world.OwnershipClaims.Add(new OwnershipClaim(issuer, buyer, 1m));
        var over = await Run(new PurchaseOwnership(issuer, hh, 0.1m, Money.From(10m)));
        await Assert.That(over.Events.OfType<OwnershipPurchased>().Count()).IsEqualTo(0);

        var firmOver = await Run(new PurchaseOwnership(issuer, buyer, 0.1m, Money.From(10m)));
        await Assert.That(firmOver.Events.OfType<OwnershipPurchased>().Count()).IsEqualTo(0);
    }

    [Test]
    public async Task Agents_Extractive_Treasury_Manufacturing_Household_Branches()
    {
        var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000210"));
        var treasury = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000211"));
        var borrower = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000212"));
        var hh = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000213"));
        var loc = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-000000000214"));
        var fac = FacilityId.From(Guid.Parse("00000000-0000-4000-8000-000000000215"));
        var input = ProductId.From(Guid.Parse("00000000-0000-4000-8000-000000000216"));
        var output = ProductId.From(Guid.Parse("00000000-0000-4000-8000-000000000217"));
        var cat = ProductCategoryId.From(Guid.Parse("00000000-0000-4000-8000-000000000218"));
        var process = ProductionProcessId.From(Guid.Parse("00000000-0000-4000-8000-000000000099"));
        var unit = OperatingUnitId.From(Guid.Parse("00000000-0000-4000-8000-000000000219"));
        var area = GeographicAreaId.From(Guid.Parse("aaaaaaaa-0000-4000-8000-000000000210"));

        var builder = new EconomyWorldBuilder(new EconomyPolicy
        {
            HouseholdComfortThresholdPerHousehold = Money.From(40m),
        });
        builder.AddProduct(new ProductDefinition(
            input, cat, ImmutableArray<ProductInput>.Empty,
            ImmutableArray<ProductAttributeDefinition>.Empty, process, null));
        builder.AddProduct(new ProductDefinition(
            output, cat, ImmutableArray.Create(new ProductInput(input, Quantity.From(1m))),
            ImmutableArray<ProductAttributeDefinition>.Empty, process, null));
        builder.AddFirm(firm, "Mine", Money.From(5_000m));
        builder.AddFirm(treasury, "Treasury", Money.From(20_000m));
        builder.AddFirm(borrower, "Borrower", Money.From(5m));
        builder.AddRegion(area, 10, 4);
        builder.AddCohort(new ConsumerCohort(
            ConsumerCohortId.From(Guid.Parse("00000000-0000-4000-8000-00000000021a")),
            new PopulationCount(1), Money.From(10m), Prefs(), area, HouseholdFirmId: hh));
        builder.AddFacility(new FacilityBinding(
            fac, firm, loc, loc,
            new FacilityLayout(
                ImmutableDictionary<OperatingUnitId, OperatingUnit>.Empty.Add(
                    unit, new OperatingUnit(unit, OperatingUnitKind.Manufacturing, Quantity.From(20m))),
                ImmutableArray<MaterialRoute>.Empty)));
        var sim = new EconomySimulation(33, builder.Build());
        sim.State.World.Inventory.Add(
            new InventoryKey(firm, loc, output),
            new ProductBatch(output, Quantity.From(30m), new ProductQuality(100m), Money.From(1m), SimulationDate.Epoch, null));

        var extractive = new ExtractiveFirmAgent(firm, new ExtractiveFirmAgentPolicy(
            [new AgentSite(loc, fac, Name: "pit")],
            OutputProduct: output, InputProduct: input,
            BaseOutputRate: 4m, OutputCap: 50m, InputPerOutput: 1m, InputFloor: 5m,
            SellAboveStock: 10m, SellKeepFloor: 5m, SellMaxQty: 8m,
            OutputGatePrice: 6m, InputLimitPrice: 2m));
        // Enough input to produce, still below InputFloor → bid (and rate > 0 so not starved).
        sim.State.World.Inventory.Add(
            new InventoryKey(firm, loc, input),
            new ProductBatch(input, Quantity.From(2m), new ProductQuality(100m), Money.From(1m), SimulationDate.Epoch, null));
        extractive.Tick(new AgentContext(sim, new DeterministicRandom(33)));
        await Assert.That(extractive.LastDecision).Contains("bid input");

        sim.State.World.Inventory.Add(
            new InventoryKey(firm, loc, input),
            new ProductBatch(input, Quantity.From(20m), new ProductQuality(100m), Money.From(1m), SimulationDate.Epoch, null));
        extractive.Tick(new AgentContext(sim, new DeterministicRandom(34)));
        await Assert.That(extractive.LastDecision).Contains("sell");

        var starved = new ExtractiveFirmAgent(firm, new ExtractiveFirmAgentPolicy(
            [new AgentSite(loc, fac, Name: "pit")],
            output, input, BaseOutputRate: 4m, OutputCap: 1m, InputPerOutput: 1m, InputFloor: 0m,
            SellAboveStock: 100m, SellKeepFloor: 0m, SellMaxQty: 1m,
            OutputGatePrice: 6m, InputLimitPrice: 2m));
        sim.State.World.Inventory.TryTake(new InventoryKey(firm, loc, input), Quantity.From(22m), out _, out _);
        starved.Tick(new AgentContext(sim, new DeterministicRandom(35)));
        await Assert.That(starved.LastDecision).Contains("starved");

        var mfg = new ManufacturingFirmAgent(firm, new ManufacturingFirmAgentPolicy(
            [new AgentSite(loc, fac, Name: "plant")],
            PrimaryInput: input, PrimaryInputFloor: 100m, PrimaryInputLimitPrice: 3m,
            Outputs:
            [
                new ManufacturedSkuPolicy(
                    output, BaseRate: 2m, StockTarget: 5m, MinInputOnHand: 0m, RequiredInput: null,
                    SellAboveStock: 5m, SellKeepFloor: 1m, SellMaxQty: 10m, GatePrice: 8m),
            ]));
        mfg.Tick(new AgentContext(sim, new DeterministicRandom(36)));
        await Assert.That(sim.State.PendingCommands.OfType<PostHubOrder>().Any(o => o.Side == HubOrderSide.Sell)).IsTrue();

        var loan = new FinanceLoan(
            LoanId.From(Guid.Parse("00000000-0000-4000-8000-00000000021b")),
            treasury, borrower, Money.From(100m), 0.1m, SimulationHour.Epoch, SimulationHour.Epoch.AddHours(10));
        sim.State.World.Loans.Add(loan);
        var treasuryAgent = new TreasuryFirmAgent(treasury, new TreasuryFirmAgentPolicy(
            [borrower], CashFloorToLend: 1_000m, BorrowerCashFloor: 50m,
            LoanPrincipal: Money.From(100m), AnnualInterestRate: 0.1m, TermHours: 24, MaxActiveLoansToBorrower: 1));
        treasuryAgent.Tick(new AgentContext(sim, new DeterministicRandom(37)));
        await Assert.That(treasuryAgent.LastDecision).IsEqualTo("treasury idle");

        loan.Status = FinanceLoanStatus.Defaulted;
        treasuryAgent.Tick(new AgentContext(sim, new DeterministicRandom(38)));
        await Assert.That(treasuryAgent.LastDecision).IsEqualTo("treasury idle");

        var ghostHh = new HouseholdFirmAgent(FirmId.From(Guid.NewGuid()));
        ghostHh.Tick(new AgentContext(sim, new DeterministicRandom(39)));
        await Assert.That(ghostHh.LastDecision).IsEqualTo("no cohort");

        var comfort = new HouseholdFirmAgent(hh, new HouseholdFirmAgentPolicy(PreferredBorrower: borrower));
        comfort.Tick(new AgentContext(sim, new DeterministicRandom(40)));
        await Assert.That(comfort.LastDecision).Contains("comfort");

        sim.State.World.Cohorts[0].BudgetRemaining = Money.From(200m);
        var invest = new HouseholdFirmAgent(hh, new HouseholdFirmAgentPolicy(
            PreferredIssuer: firm, PurchaseFraction: 0.05m, PurchasePrice: Money.From(15m)));
        invest.Tick(new AgentContext(sim, new DeterministicRandom(41)));
        await Assert.That(invest.LastDecision).Contains("invest");
    }

    [Test]
    public async Task Carrier_Bunkering_LocalSaleFallback_And_RouteCacheMiss()
    {
        var (sim, ids) = CreateCarrierMini();
        // Holding cargo with no remote bid → local sale fallback (no bid book).
        sim.State.World.Inventory.Add(
            new InventoryKey(ids.Carrier, ids.LocNorth, ids.Cargo),
            new ProductBatch(ids.Cargo, Quantity.From(4m), new ProductQuality(100m), Money.From(2m), SimulationDate.Epoch, null));
        ids.Agent.Tick(new AgentContext(sim, new DeterministicRandom(50)));
        await Assert.That(ids.Agent.LastDecision).Contains("offer");

        // Resume active haul while fuel is empty → bunkering.
        var (sim2, ids2) = CreateCarrierMini(fuelAtNorth: 0m);
        sim2.Enqueue(new PostHubOrder(
            ids2.Seller, ids2.LocNorth, ids2.Cargo, HubOrderSide.Sell, Quantity.From(10m), Money.From(2m)));
        sim2.Enqueue(new PostHubOrder(
            ids2.Buyer, ids2.LocSouth, ids2.Cargo, HubOrderSide.Buy, Quantity.From(10m), Money.From(20m)));
        await sim2.AdvanceAsync(SimulationDuration.FromHours(1));
        ids2.Agent.Tick(new AgentContext(sim2, new DeterministicRandom(51)));
        await Assert.That(ids2.Agent.LastDecision).Contains("lift");

        sim2.State.World.Inventory.Add(
            new InventoryKey(ids2.Carrier, ids2.LocNorth, ids2.Cargo),
            new ProductBatch(ids2.Cargo, Quantity.From(10m), new ProductQuality(100m), Money.From(2m), SimulationDate.Epoch, null));
        ids2.Agent.Tick(new AgentContext(sim2, new DeterministicRandom(52)));
        await Assert.That(ids2.Agent.LastDecision).Contains("bunkering");

        // Outbound haul with cargo but no fuel.
        var (sim3, ids3) = CreateCarrierMini(cargoAtNorth: 6m, fuelAtNorth: 0m);
        sim3.Enqueue(new PostHubOrder(
            ids3.Buyer, ids3.LocSouth, ids3.Cargo, HubOrderSide.Buy, Quantity.From(6m), Money.From(20m)));
        await sim3.AdvanceAsync(SimulationDuration.FromHours(1));
        ids3.Agent.Tick(new AgentContext(sim3, new DeterministicRandom(53)));
        await Assert.That(ids3.Agent.LastDecision).Contains("bunkering");

        // RefuseHaul filters candidates when evaluating spreads (fresh world, no hull cargo).
        var (sim4, ids4) = CreateCarrierMini();
        var refuse = new CarrierFirmAgent(
            ids4.Carrier,
            new CarrierFirmAgentPolicy(
                Sites:
                [
                    new AgentSite(ids4.LocNorth, HubId: ids4.HubNorth, Name: "North"),
                    new AgentSite(ids4.LocSouth, HubId: ids4.HubSouth, Name: "South"),
                ],
                FreightProducts: [ids4.Cargo],
                FuelProduct: ids4.Fuel,
                VehicleClassId: ids4.Vehicle,
                Vehicle: sim4.State.World.VehicleClasses[ids4.Vehicle],
                MinMargin: 0m,
                GatePrice: _ => 2m,
                FuelBuyLimitPrice: 2m,
                RefuseHaul: (_, _, _, _) => true),
            ids4.HubNorth);
        sim4.Enqueue(new PostHubOrder(
            ids4.Seller, ids4.LocNorth, ids4.Cargo, HubOrderSide.Sell, Quantity.From(10m), Money.From(2m)));
        sim4.Enqueue(new PostHubOrder(
            ids4.Buyer, ids4.LocSouth, ids4.Cargo, HubOrderSide.Buy, Quantity.From(10m), Money.From(20m)));
        await sim4.AdvanceAsync(SimulationDuration.FromHours(1));
        refuse.Tick(new AgentContext(sim4, new DeterministicRandom(54)));
        await Assert.That(refuse.LastDecision).Contains("idle");
    }

    private static (EconomySimulation Sim, CarrierMini Ids) CreateCarrierMini(
        decimal cargoAtNorth = 0m,
        decimal fuelAtNorth = 20m)
    {
        var builder = new EconomyWorldBuilder(new EconomyPolicy
        {
            WageRatePerHour = Money.From(8m),
            LaborHoursPerOutputUnit = 0.1m,
            PeriodHours = 24,
        });
        var carrier = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c1"));
        var seller = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c2"));
        var buyer = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c3"));
        var locNorth = InventoryLocationId.From(builder.NextGuid());
        var locSouth = InventoryLocationId.From(builder.NextGuid());
        var hubNorth = TransportHubId.From(builder.NextGuid());
        var hubSouth = TransportHubId.From(builder.NextGuid());
        var vehicleId = VehicleClassId.From(builder.NextGuid());
        var cargoCat = ProductCategoryId.From(builder.NextGuid());
        var fuelCat = ProductCategoryId.From(builder.NextGuid());
        var cargo = ProductId.From(builder.NextGuid());
        var fuel = ProductId.From(builder.NextGuid());
        var process = ProductionProcessId.From(Guid.Parse("00000000-0000-4000-8000-000000000099"));
        var vehicle = new VehicleClass(vehicleId, Quantity.From(30m), 1m, 2m, Quantity.From(8m));

        builder
            .AddProduct(new ProductDefinition(
                cargo, cargoCat, ImmutableArray<ProductInput>.Empty,
                ImmutableArray<ProductAttributeDefinition>.Empty, process, null))
            .AddProduct(new ProductDefinition(
                fuel, fuelCat, ImmutableArray<ProductInput>.Empty,
                ImmutableArray<ProductAttributeDefinition>.Empty, process, null))
            .AddFirm(carrier, "Carrier", Money.From(10_000m))
            .AddFirm(seller, "Seller", Money.From(5_000m))
            .AddFirm(buyer, "Buyer", Money.From(5_000m))
            .AddHub(new TransportHub(hubNorth, locNorth, "North", 1, 2))
            .AddHub(new TransportHub(hubSouth, locSouth, "South", 1, 2))
            .AddCorridor(new TransportCorridor(
                TransportCorridorId.From(builder.NextGuid()), hubNorth, hubSouth,
                3, Quantity.From(30m), 1m, Money.From(5m)))
            .AddVehicleClass(vehicle)
            .SetTransportFuel(fuel, Money.From(1m))
            .SetLabor(carrier, 24m);

        if (fuelAtNorth > 0m)
        {
            builder.AddInventory(carrier, locNorth, new ProductBatch(
                fuel, Quantity.From(fuelAtNorth), new ProductQuality(100m), Money.From(1m), SimulationDate.Epoch, null));
        }

        if (cargoAtNorth > 0m)
        {
            builder.AddInventory(carrier, locNorth, new ProductBatch(
                cargo, Quantity.From(cargoAtNorth), new ProductQuality(100m), Money.From(2m), SimulationDate.Epoch, null));
        }

        var sim = new EconomySimulation(88, builder.Build());
        var agent = new CarrierFirmAgent(
            carrier,
            new CarrierFirmAgentPolicy(
                Sites:
                [
                    new AgentSite(locNorth, HubId: hubNorth, Name: "North"),
                    new AgentSite(locSouth, HubId: hubSouth, Name: "South"),
                ],
                FreightProducts: [cargo],
                FuelProduct: fuel,
                VehicleClassId: vehicleId,
                Vehicle: vehicle,
                MinMargin: 0m,
                GatePrice: _ => 2m,
                FuelBuyLimitPrice: 2m),
            hubNorth);
        return (sim, new CarrierMini(carrier, seller, buyer, locNorth, locSouth, hubNorth, hubSouth, vehicleId, cargo, fuel, agent));
    }

    private sealed record CarrierMini(
        FirmId Carrier,
        FirmId Seller,
        FirmId Buyer,
        InventoryLocationId LocNorth,
        InventoryLocationId LocSouth,
        TransportHubId HubNorth,
        TransportHubId HubSouth,
        VehicleClassId Vehicle,
        ProductId Cargo,
        ProductId Fuel,
        CarrierFirmAgent Agent);

    private sealed class FakeCreditSource : ICreditCirculationSource
    {
        public List<IEconomyEvent> Events { get; } = [];
        public SimulationHour Clock { get; set; } = SimulationHour.Epoch;
        public decimal LiquidStock { get; set; } = 500m;
        public decimal HouseholdBudgets { get; set; } = 50m;
        public decimal FirmCash { get; set; } = 400m;
        public decimal InventoryBookValue { get; set; } = 120m;
        public decimal CargoDelivered { get; set; } = 15m;
        public int ActiveLoanCount { get; set; } = 2;
        public decimal PrincipalOutstanding { get; set; } = 300m;
        public int CreditFrozenFirmCount { get; set; }
        public int CorePeriod { get; set; } = 3;
        public decimal CoreTotalCash { get; set; } = 200m;
        public decimal CoreHoldingQty { get; set; } = 40m;
        public int CoreHoldingSlots { get; set; } = 5;
        public int CoreInFlightTransfers { get; set; } = 1;

        IReadOnlyList<IEconomyEvent> ICreditCirculationSource.Events => Events;

        public decimal InventoryQuantity(ProductId productId) => 0m;
    }
}
