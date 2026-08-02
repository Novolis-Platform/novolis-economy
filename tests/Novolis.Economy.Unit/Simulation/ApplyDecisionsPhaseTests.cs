using System.Collections.Immutable;
using Novolis.Economy;
using Novolis.Economy.Accounting;
using Novolis.Economy.Finance;
using Novolis.Economy.Logistics;
using Novolis.Economy.Markets;
using Novolis.Economy.Population;
using Novolis.Economy.Production;
using Novolis.Economy.Simulation;
using Novolis.Economy.Simulation.Phases;

namespace Novolis.Economy.Unit.Simulation;

public sealed class ApplyDecisionsPhaseTests
{
    private static readonly ApplyDecisionsPhase Phase = new();

    private static async Task<SimulationState> RunAsync(params IEconomyCommand[] commands)
    {
        var state = new SimulationState(11, new EconomyWorldBuilder().Build());
        foreach (var cmd in commands)
            state.EnqueueCommand(cmd);
        var ctx = new SimulationContext(state, new DeterministicRandom(11));
        await Phase.ExecuteAsync(ctx, CancellationToken.None);
        return state;
    }

    private static async Task<SimulationState> RunWithWorldAsync(EconomyWorld world, params IEconomyCommand[] commands)
    {
        var state = new SimulationState(11, world);
        foreach (var cmd in commands)
            state.EnqueueCommand(cmd);
        var ctx = new SimulationContext(state, new DeterministicRandom(11));
        await Phase.ExecuteAsync(ctx, CancellationToken.None);
        return state;
    }

    [Test]
    public async Task SetProductionPlan_RecordsPlanAndEvent()
    {
        var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a1"));
        var facility = FacilityId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a2"));
        var product = ProductId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a3"));
        var state = await RunAsync(new SetProductionPlan(firm, facility, product, Quantity.From(3m)));

        await Assert.That(state.World.ProductionPlans[(firm, facility, product)].Value).IsEqualTo(3m);
        await Assert.That(state.Events.OfType<ProductionPlanSet>().Count()).IsEqualTo(1);
    }

    [Test]
    public async Task QueuesProcurementExportShipmentAndLabor()
    {
        var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000b1"));
        var loc = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-0000000000b2"));
        var product = ProductId.From(Guid.Parse("00000000-0000-4000-8000-0000000000b3"));
        var route = FreightRouteId.From(Guid.Parse("00000000-0000-4000-8000-0000000000b4"));

        var state = await RunAsync(
            new PlaceProcurementOrder(firm, loc, product, Quantity.From(5m), Money.From(2m)),
            new PlaceExportOrder(firm, loc, product, Quantity.From(4m), Money.From(3m)),
            new IssueShipment(firm, FreightRouteId.From(route.Value), product, Quantity.From(2m)),
            new SetAvailableLabor(firm, 12m));

        await Assert.That(state.World.PendingProcurement.Count).IsEqualTo(1);
        await Assert.That(state.World.PendingExports.Count).IsEqualTo(1);
        await Assert.That(state.World.PendingShipments.Count).IsEqualTo(1);
        await Assert.That(state.World.AvailableLaborHours[firm]).IsEqualTo(12m);
    }

    [Test]
    public async Task PostAndCancelHubOrder()
    {
        var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c1"));
        var loc = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c2"));
        var product = ProductId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c3"));
        var world = new EconomyWorldBuilder().AddFirm(firm, "F", Money.From(100m)).Build();
        var state = new SimulationState(11, world);
        state.EnqueueCommand(new PostHubOrder(firm, loc, product, HubOrderSide.Sell, Quantity.From(10m), Money.From(5m)));
        var ctx = new SimulationContext(state, new DeterministicRandom(11));
        await Phase.ExecuteAsync(ctx, CancellationToken.None);
        var orderId = state.World.HubOrders.Single().Id;

        state.EnqueueCommand(new CancelHubOrder(orderId));
        await Phase.ExecuteAsync(ctx, CancellationToken.None);

        await Assert.That(state.World.HubOrders.Count).IsEqualTo(0);
    }

    [Test]
    public async Task TransferGoodsForCash_Succeeds()
    {
        var (seller, buyer, loc, product, world) = BuildTransferWorld();
        var state = await RunWithWorldAsync(world, new TransferGoodsForCash(
            seller, buyer, loc, product, Quantity.From(3m), Money.From(4m)));
        await Assert.That(state.Events.OfType<GoodsSoldInterFirm>().Count()).IsEqualTo(1);
        await Assert.That(state.World.Inventory.GetQuantity(new InventoryKey(buyer, loc, product)).Value).IsEqualTo(3m);
    }

    [Test]
    public async Task TransferGoodsForCash_FailsOnStockAndCash()
    {
        var (seller, buyer, loc, product, stockWorld) = BuildTransferWorld(stockQty: 5m);
        var stockFail = await RunWithWorldAsync(stockWorld, new TransferGoodsForCash(
            seller, buyer, loc, product, Quantity.From(10m), Money.From(4m)));
        await Assert.That(stockFail.Events.OfType<TransferGoodsFailed>().Any(e => e.Reason == "stock")).IsTrue();

        var (s2, b2, l2, p2, cashWorld) = BuildTransferWorld(stockQty: 8m, buyerCash: 0m);
        var cashFail = await RunWithWorldAsync(cashWorld, new TransferGoodsForCash(
            s2, b2, l2, p2, Quantity.From(1m), Money.From(4m)));
        await Assert.That(cashFail.Events.OfType<TransferGoodsFailed>().Any(e => e.Reason == "cash")).IsTrue();
    }

    private static (FirmId Seller, FirmId Buyer, InventoryLocationId Loc, ProductId Product, EconomyWorld World) BuildTransferWorld(
        decimal stockQty = 8m,
        decimal buyerCash = 100m)
    {
        var seller = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d1"));
        var buyer = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d2"));
        var loc = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d3"));
        var product = ProductId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d4"));
        var cat = ProductCategoryId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d5"));
        var process = ProductionProcessId.From(Guid.Parse("00000000-0000-4000-8000-000000000099"));

        var builder = new EconomyWorldBuilder();
        builder.AddProduct(new ProductDefinition(
            product, cat, ImmutableArray<ProductInput>.Empty,
            ImmutableArray<ProductAttributeDefinition>.Empty, process, null));
        builder.AddFirm(seller, "Seller", Money.From(10m));
        builder.AddFirm(buyer, "Buyer", Money.From(buyerCash));
        var world = builder.Build();
        world.Inventory.Add(
            new InventoryKey(seller, loc, product),
            new ProductBatch(product, Quantity.From(stockQty), new ProductQuality(100m), Money.From(2m), SimulationDate.Epoch, null));
        return (seller, buyer, loc, product, world);
    }

    [Test]
    public async Task OriginateLoan_RepayLoan_AndFrozenBorrower()
    {
        var lender = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000e1"));
        var borrower = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000e2"));
        var builder = new EconomyWorldBuilder();
        builder.AddFirm(lender, "Bank", Money.From(10_000m));
        builder.AddFirm(borrower, "Mine", Money.From(100m));
        var world = builder.Build();

        var originated = await RunWithWorldAsync(world, new OriginateLoan(
            lender, borrower, Money.From(500m), 0.1m, TermHours: 240));
        await Assert.That(originated.Events.OfType<LoanOriginated>().Count()).IsEqualTo(1);
        var loanId = originated.World.Loans.Single().Id;

        var repaid = await RunWithWorldAsync(originated.World, new RepayLoan(loanId, Money.From(100m)));
        await Assert.That(repaid.Events.OfType<LoanRepaid>().Count()).IsEqualTo(1);

        originated.World.Entities[borrower].CreditFrozen = true;
        var blocked = await RunWithWorldAsync(originated.World, new OriginateLoan(
            lender, borrower, Money.From(50m), 0.1m, TermHours: 24));
        await Assert.That(blocked.World.Loans.Count).IsEqualTo(1);
    }

    [Test]
    public async Task OwnershipCommands_AssignTransferAndDividend()
    {
        var issuer = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000f1"));
        var ownerA = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000f2"));
        var ownerB = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000f3"));
        var world = new EconomyWorldBuilder()
            .AddFirm(issuer, "Issuer", Money.From(1_000m))
            .AddCivic(ownerA, "OwnerA", Money.From(100m), "reg")
            .AddFirm(ownerB, "OwnerB", Money.From(100m))
            .Build();

        var assigned = await RunWithWorldAsync(world,
            new AssignOwnership(issuer, ownerA, 0.6m),
            new AssignOwnership(issuer, ownerB, 0.4m));
        await Assert.That(assigned.Events.OfType<OwnershipChanged>().Count()).IsEqualTo(2);

        var transferred = await RunWithWorldAsync(assigned.World,
            new TransferOwnership(issuer, ownerA, ownerB, 0.2m));
        await Assert.That(transferred.Events.OfType<OwnershipChanged>().Count()).IsEqualTo(2);

        var dividend = await RunWithWorldAsync(transferred.World, new DeclareDividend(issuer, Money.From(200m)));
        await Assert.That(dividend.Events.OfType<DividendPaid>().Count()).IsGreaterThan(0);
    }

    [Test]
    public async Task UpgradeFacility_EmitsSuccessOrFailure()
    {
        var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000010"));
        var facilityId = FacilityId.From(Guid.Parse("00000000-0000-4000-8000-000000000011"));
        var unitId = OperatingUnitId.From(Guid.Parse("00000000-0000-4000-8000-000000000012"));
        var loc = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-000000000013"));
        var layout = new FacilityLayout(
            ImmutableDictionary<OperatingUnitId, OperatingUnit>.Empty.Add(
                unitId, new OperatingUnit(unitId, OperatingUnitKind.Manufacturing, Quantity.From(10m))),
            ImmutableArray<MaterialRoute>.Empty);
        var world = new EconomyWorldBuilder()
            .AddFirm(firm, "Plant", Money.From(200m))
            .AddFacility(new FacilityBinding(facilityId, firm, loc, null, layout))
            .Build();

        var ok = await RunWithWorldAsync(world, new UpgradeFacility(facilityId, Money.From(50m), 1.25m));
        await Assert.That(ok.Events.OfType<FacilityUpgraded>().Count()).IsEqualTo(1);

        var fail = await RunWithWorldAsync(ok.World, new UpgradeFacility(facilityId, Money.From(50_000m), 1.25m));
        await Assert.That(fail.Events.OfType<FacilityUpgradeFailed>().Count()).IsEqualTo(1);
    }

    private static PreferenceProfile HouseholdPrefs() =>
        new(
            ImmutableArray<CategoryPreference>.Empty,
            PriceSensitivity: 1m,
            QualitySensitivity: 1m,
            BrandLoyalty: 0m);

    [Test]
    public async Task HouseholdOriginateLoan_BlocksBelowComfortAndRollsBackOnLedgerFail()
    {
        var area = GeographicAreaId.From(Guid.Parse("aaaaaaaa-0000-4000-8000-000000000100"));
        var hh = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000100"));
        var borrower = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000101"));
        var ghostBorrower = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000102"));

        var builder = new EconomyWorldBuilder(new EconomyPolicy
        {
            HouseholdComfortThresholdPerHousehold = Money.From(50m),
            CohortBudgetResetMode = CohortBudgetResetMode.CarryForward,
        });
        builder.AddRegion(area, 10, 4);
        builder.AddFirm(borrower, "Borrower", Money.From(10m));
        builder.AddCohort(new ConsumerCohort(
            ConsumerCohortId.From(Guid.Parse("00000000-0000-4000-8000-000000000103")),
            new PopulationCount(1),
            Money.From(100m),
            HouseholdPrefs(),
            area,
            HouseholdFirmId: hh));
        var world = builder.Build();

        var belowComfort = await RunWithWorldAsync(world, new OriginateLoan(hh, borrower, Money.From(55m), 0.1m, TermHours: 24));
        await Assert.That(belowComfort.World.Loans.Count).IsEqualTo(0);
        await Assert.That(belowComfort.World.Cohorts[0].BudgetRemaining.Amount).IsEqualTo(100m);

        var rollback = await RunWithWorldAsync(world, new OriginateLoan(hh, ghostBorrower, Money.From(20m), 0.1m, TermHours: 24));
        await Assert.That(rollback.World.Loans.Count).IsEqualTo(0);
        await Assert.That(rollback.World.Cohorts[0].BudgetRemaining.Amount).IsEqualTo(100m);
    }

    [Test]
    public async Task HouseholdOriginateLoan_SucceedsAndRepayCreditsBudget()
    {
        var area = GeographicAreaId.From(Guid.Parse("aaaaaaaa-0000-4000-8000-000000000110"));
        var hh = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000110"));
        var borrower = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000111"));

        var builder = new EconomyWorldBuilder(new EconomyPolicy
        {
            HouseholdComfortThresholdPerHousehold = Money.From(50m),
            CohortBudgetResetMode = CohortBudgetResetMode.CarryForward,
        });
        builder.AddRegion(area, 10, 4);
        builder.AddFirm(borrower, "Borrower", Money.From(500m));
        builder.AddCohort(new ConsumerCohort(
            ConsumerCohortId.From(Guid.Parse("00000000-0000-4000-8000-000000000112")),
            new PopulationCount(1),
            Money.From(200m),
            HouseholdPrefs(),
            area,
            HouseholdFirmId: hh));
        var world = builder.Build();

        var originated = await RunWithWorldAsync(world, new OriginateLoan(hh, borrower, Money.From(40m), 0.1m, TermHours: 48));
        await Assert.That(originated.Events.OfType<LoanOriginated>().Count()).IsEqualTo(1);
        await Assert.That(originated.World.Cohorts[0].BudgetRemaining.Amount).IsEqualTo(160m);
        var loanId = originated.World.Loans.Single().Id;

        var repaid = await RunWithWorldAsync(originated.World, new RepayLoan(loanId, Money.From(10m)));
        await Assert.That(repaid.Events.OfType<LoanRepaid>().Count()).IsEqualTo(1);
    }

    [Test]
    public async Task PurchaseOwnership_FirmBuyerInsufficientCashAndInvalidPost()
    {
        var issuer = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000120"));
        var buyer = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000121"));
        var loc = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-000000000122"));
        var product = ProductId.From(Guid.Parse("00000000-0000-4000-8000-000000000123"));

        var world = new EconomyWorldBuilder()
            .AddFirm(issuer, "Issuer", Money.From(100m))
            .AddFirm(buyer, "Buyer", Money.From(5m))
            .Build();

        var blocked = await RunWithWorldAsync(world,
            new PurchaseOwnership(issuer, buyer, 0.1m, Money.From(50m)),
            new PostHubOrder(buyer, loc, product, HubOrderSide.Buy, Quantity.From(0m), Money.From(5m)));
        await Assert.That(blocked.Events.OfType<OwnershipPurchased>().Count()).IsEqualTo(0);
        await Assert.That(blocked.World.HubOrders.Count).IsEqualTo(0);
    }

    [Test]
    public async Task PurchaseOwnership_FirmBuyerSucceeds()
    {
        var issuer = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000125"));
        var buyer = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000126"));
        var world = new EconomyWorldBuilder()
            .AddFirm(issuer, "Issuer", Money.From(50m))
            .AddFirm(buyer, "Buyer", Money.From(200m))
            .Build();

        var ok = await RunWithWorldAsync(world, new PurchaseOwnership(issuer, buyer, 0.15m, Money.From(30m)));
        await Assert.That(ok.Events.OfType<OwnershipPurchased>().Count()).IsEqualTo(1);
        await Assert.That(ok.World.OwnershipClaims.Single().Fraction).IsEqualTo(0.15m);
        await Assert.That(ok.World.Ledgers[buyer].Cash.Amount).IsEqualTo(170m);
        await Assert.That(ok.World.Ledgers[issuer].Cash.Amount).IsEqualTo(80m);
    }

    [Test]
    public async Task PurchaseOwnership_RejectsInvalidIssuerOrFraction()
    {
        var hh = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000127"));
        var issuer = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000128"));
        var area = GeographicAreaId.From(Guid.Parse("aaaaaaaa-0000-4000-8000-000000000127"));
        var builder = new EconomyWorldBuilder();
        builder.AddRegion(area, 10, 4);
        builder.AddFirm(issuer, "Issuer", Money.From(100m));
        builder.AddCohort(new ConsumerCohort(
            ConsumerCohortId.From(Guid.Parse("00000000-0000-4000-8000-000000000129")),
            new PopulationCount(1),
            Money.From(200m),
            HouseholdPrefs(),
            area,
            HouseholdFirmId: hh));
        var world = builder.Build();

        var blocked = await RunWithWorldAsync(world, new PurchaseOwnership(hh, hh, 0.1m, Money.From(10m)));
        await Assert.That(blocked.Events.OfType<OwnershipPurchased>().Count()).IsEqualTo(0);
    }

    [Test]
    public async Task TransferGoodsForCash_InvalidQuantityAndMissingLedger()
    {
        var seller = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000130"));
        var buyer = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000131"));
        var ghost = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000132"));
        var loc = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-000000000133"));
        var product = ProductId.From(Guid.Parse("00000000-0000-4000-8000-000000000134"));

        var world = new EconomyWorldBuilder()
            .AddFirm(seller, "Seller", Money.From(10m))
            .Build();

        var invalid = await RunWithWorldAsync(world, new TransferGoodsForCash(
            seller, buyer, loc, product, Quantity.From(0m), Money.From(1m)));
        await Assert.That(invalid.Events.OfType<TransferGoodsFailed>().Any(e => e.Reason == "invalid")).IsTrue();

        var ledgerFail = await RunWithWorldAsync(world, new TransferGoodsForCash(
            ghost, seller, loc, product, Quantity.From(1m), Money.From(1m)));
        await Assert.That(ledgerFail.Events.OfType<TransferGoodsFailed>().Any(e => e.Reason == "ledger")).IsTrue();
    }

    [Test]
    public async Task AccountingPeriodClose_IsIgnored()
    {
        var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000150"));
        var state = await RunAsync(new AccountingPeriodClose(firm, SimulationDate.Epoch));
        await Assert.That(state.Events.Count).IsEqualTo(0);
    }

    [Test]
    public async Task SetRetailPrice_RecordsChange()
    {
        var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000140"));
        var facility = FacilityId.From(Guid.Parse("00000000-0000-4000-8000-000000000141"));
        var product = ProductId.From(Guid.Parse("00000000-0000-4000-8000-000000000142"));
        var state = await RunAsync(new SetRetailPrice(firm, facility, product, Money.From(9m)));
        await Assert.That(state.World.RetailPrices[(firm, facility, product)].Amount).IsEqualTo(9m);
        await Assert.That(state.Events.OfType<RetailPriceChanged>().Count()).IsEqualTo(1);
    }
}
