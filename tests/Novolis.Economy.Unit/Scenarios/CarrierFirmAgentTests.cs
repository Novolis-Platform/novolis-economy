using System.Collections.Immutable;
using Novolis.Economy;
using Novolis.Economy.Agents;
using Novolis.Economy.Logistics;
using Novolis.Economy.Markets;
using Novolis.Economy.Production;
using Novolis.Economy.Simulation;

namespace Novolis.Economy.Unit.Scenarios;

public sealed class CarrierFirmAgentTests
{
    [Test]
    public async Task SpreadJob_LiftsAndHauls_WhenBuyExceedsSell()
    {
        var (sim, ids) = CreateCarrierWorld();

        sim.Enqueue(new PostHubOrder(
            ids.Seller, ids.LocNorth, ids.Cargo, HubOrderSide.Sell, Quantity.From(10m), Money.From(2m)));
        sim.Enqueue(new PostHubOrder(
            ids.Buyer, ids.LocSouth, ids.Cargo, HubOrderSide.Buy, Quantity.From(10m), Money.From(12m)));
        await sim.AdvanceAsync(SimulationDuration.FromHours(1));

        ids.Agent.Tick(Ctx(sim, 101ul));

        await Assert.That(ids.Agent.LastDecision).Contains("lift");
        await Assert.That(sim.State.PendingCommands.OfType<PlanShipment>().Any(p => p.FirmId.Equals(ids.Carrier))).IsTrue();
    }

    [Test]
    public async Task OutboundHaul_MovesCargo_WhenHoldingAtOrigin()
    {
        var (sim, ids) = CreateCarrierWorld(cargoAtNorth: 8m);
        sim.Enqueue(new PostHubOrder(
            ids.Buyer, ids.LocSouth, ids.Cargo, HubOrderSide.Buy, Quantity.From(8m), Money.From(15m)));
        await sim.AdvanceAsync(SimulationDuration.FromHours(1));

        ids.Agent.Tick(Ctx(sim, 102ul));

        await Assert.That(ids.Agent.LastDecision).Contains("haul");
        await Assert.That(sim.State.PendingCommands.OfType<PlanShipment>().Any(p => p.FirmId.Equals(ids.Carrier))).IsTrue();
    }

    [Test]
    public async Task LocalSale_OffersInventory_WhenBidMeetsGate()
    {
        var (sim, ids) = CreateCarrierWorld(cargoAtNorth: 5m);
        sim.Enqueue(new PostHubOrder(
            ids.Buyer, ids.LocNorth, ids.Cargo, HubOrderSide.Buy, Quantity.From(5m), Money.From(6m)));
        await sim.AdvanceAsync(SimulationDuration.FromHours(1));

        ids.Agent.Tick(Ctx(sim, 103ul));

        await Assert.That(ids.Agent.LastDecision).Contains("offer");
        await Assert.That(sim.State.PendingCommands.OfType<PostHubOrder>().Any(o =>
            o.FirmId.Equals(ids.Carrier) && o.Side == HubOrderSide.Sell)).IsTrue();
    }

    [Test]
    public async Task Underway_ReportsTransit_WhenShipmentActive()
    {
        var (sim, ids) = CreateCarrierWorld(cargoAtNorth: 10m);
        sim.Enqueue(new PlanShipment(
            ids.Carrier, ids.HubNorth.Value, ids.HubSouth.Value,
            ids.Cargo, Quantity.From(10m), ids.Vehicle.Value));
        await sim.AdvanceAsync(SimulationDuration.FromHours(3));

        ids.Agent.Tick(Ctx(sim, 104ul));

        await Assert.That(ids.Agent.LastDecision).Contains("underway");
    }

    [Test]
    public async Task RegistryHold_SkipsDecisions_WhenCanOperateFalse()
    {
        var (sim, ids) = CreateCarrierWorld(canOperate: false);
        ids.Agent.Tick(Ctx(sim, 105ul));
        await Assert.That(ids.Agent.LastDecision).Contains("registry hold");
    }

    [Test]
    public async Task Idle_WhenSpreadBelowMinMargin()
    {
        var (sim, ids) = CreateCarrierWorld(minMargin: 10_000m);
        sim.Enqueue(new PostHubOrder(
            ids.Seller, ids.LocNorth, ids.Cargo, HubOrderSide.Sell, Quantity.From(10m), Money.From(2m)));
        sim.Enqueue(new PostHubOrder(
            ids.Buyer, ids.LocSouth, ids.Cargo, HubOrderSide.Buy, Quantity.From(10m), Money.From(3m)));
        await sim.AdvanceAsync(SimulationDuration.FromHours(1));

        ids.Agent.Tick(Ctx(sim, 106ul));

        await Assert.That(ids.Agent.LastDecision).Contains("idle");
        await Assert.That(ids.Agent.LastEval).Contains("below min");
    }

    [Test]
    public async Task AvoidHub_SkipsBlockedDestination()
    {
        var (sim, ids) = CreateCarrierWorld(avoidSouth: true);
        sim.Enqueue(new PostHubOrder(
            ids.Seller, ids.LocNorth, ids.Cargo, HubOrderSide.Sell, Quantity.From(10m), Money.From(2m)));
        sim.Enqueue(new PostHubOrder(
            ids.Buyer, ids.LocSouth, ids.Cargo, HubOrderSide.Buy, Quantity.From(10m), Money.From(20m)));
        await sim.AdvanceAsync(SimulationDuration.FromHours(1));

        ids.Agent.Tick(Ctx(sim, 107ul));

        await Assert.That(ids.Agent.LastDecision).Contains("idle");
    }

    [Test]
    public async Task AwaitingDeparture_SkipsWhilePlanQueued()
    {
        var (sim, ids) = CreateCarrierWorld();
        sim.State.World.PendingPlanShipments.Add(new PlanShipment(
            ids.Carrier, ids.HubNorth.Value, ids.HubSouth.Value,
            ids.Cargo, Quantity.From(5m), ids.Vehicle.Value));

        ids.Agent.Tick(Ctx(sim, 108ul));

        await Assert.That(ids.Agent.LastDecision).Contains("awaiting departure");
    }

    [Test]
    public async Task MidRouteLoading_BunkersAtCurrentHub()
    {
        var (sim, ids) = CreateCarrierWorld(cargoAtNorth: 0m, fuelAtNorth: 0m, fuelAtTransfer: 0m);
        var vehicle = sim.State.World.VehicleClasses[ids.Vehicle];
        ItineraryPlanner.TryPlan(
            ids.HubNorth, ids.HubSouth, vehicle.CargoCapacity, vehicle,
            sim.State.World.Corridors, out var itinerary, TransitProfile.StandardCommercial);

        sim.State.World.Shipments.Add(new ActiveShipment(
            ShipmentId.From(Guid.Parse("00000000-0000-4000-8000-000000000201")),
            ids.Carrier,
            ids.Cargo,
            Quantity.From(5m),
            Money.From(2m),
            SimulationHour.Epoch,
            itinerary,
            vehicle,
            ids.HubNorth,
            ids.Fuel)
        {
            Phase = ShipmentPhase.Loading,
            HubStallHours = 2,
        });

        ids.Agent.Tick(Ctx(sim, 109ul));

        await Assert.That(ids.Agent.LastDecision).Contains("bunker mid-route");
        await Assert.That(
            sim.State.PendingCommands.OfType<PlaceProcurementOrder>().Any()
            || sim.State.PendingCommands.OfType<PostHubOrder>().Any(o => o.ProductId.Equals(ids.Fuel))).IsTrue();
    }

    [Test]
    public async Task WaitingBerth_ReportsAwaitBerth()
    {
        var (sim, ids) = CreateCarrierWorld();
        var vehicle = sim.State.World.VehicleClasses[ids.Vehicle];
        sim.State.World.Shipments.Add(new ActiveShipment(
            ShipmentId.From(Guid.Parse("00000000-0000-4000-8000-000000000202")),
            ids.Carrier,
            ids.Cargo,
            Quantity.From(5m),
            Money.From(2m),
            SimulationHour.Epoch,
            Itinerary.Empty,
            vehicle,
            ids.HubNorth,
            ids.Fuel)
        {
            Phase = ShipmentPhase.WaitingBerth,
        });

        ids.Agent.Tick(Ctx(sim, 110ul));

        await Assert.That(ids.Agent.LastDecision).Contains("await berth");
    }

    [Test]
    public async Task RefuseHaulAndEffectiveMinMargin_FilterJobs()
    {
        var (sim, ids) = CreateCarrierWorld(minMargin: 0m);
        var agent = new CarrierFirmAgent(
            ids.Carrier,
            new CarrierFirmAgentPolicy(
                Sites:
                [
                    new AgentSite(ids.LocNorth, HubId: ids.HubNorth, Name: "North"),
                    new AgentSite(ids.LocSouth, HubId: ids.HubSouth, Name: "South"),
                ],
                FreightProducts: [ids.Cargo],
                FuelProduct: ids.Fuel,
                VehicleClassId: ids.Vehicle,
                Vehicle: sim.State.World.VehicleClasses[ids.Vehicle],
                MinMargin: 0m,
                GatePrice: _ => 2m,
                FuelBuyLimitPrice: 2m,
                EffectiveMinMargin: () => 1_000_000m,
                RefuseHaul: (_, dest, _, _) => dest.Equals(ids.HubSouth),
                ChooseTransitProfile: _ => TransitProfile.SlowEconomic),
            ids.HubNorth);

        sim.Enqueue(new PostHubOrder(
            ids.Seller, ids.LocNorth, ids.Cargo, HubOrderSide.Sell, Quantity.From(10m), Money.From(2m)));
        sim.Enqueue(new PostHubOrder(
            ids.Buyer, ids.LocSouth, ids.Cargo, HubOrderSide.Buy, Quantity.From(10m), Money.From(20m)));
        await sim.AdvanceAsync(SimulationDuration.FromHours(1));

        agent.Tick(Ctx(sim, 111ul));

        await Assert.That(agent.LastDecision).Contains("idle");
        await Assert.That(agent.CurrentHub).IsEqualTo(ids.HubNorth);
    }

    [Test]
    public async Task LiftSpread_AwaitsBunkerWhenFuelLow()
    {
        var (sim, ids) = CreateCarrierWorld(cargoAtNorth: 0m, minMargin: 0m, fuelAtNorth: 0m, fuelAtTransfer: 0m);

        sim.Enqueue(new PostHubOrder(
            ids.Seller, ids.LocNorth, ids.Cargo, HubOrderSide.Sell, Quantity.From(10m), Money.From(2m)));
        sim.Enqueue(new PostHubOrder(
            ids.Buyer, ids.LocSouth, ids.Cargo, HubOrderSide.Buy, Quantity.From(10m), Money.From(20m)));
        await sim.AdvanceAsync(SimulationDuration.FromHours(1));

        ids.Agent.Tick(Ctx(sim, 112ul));

        await Assert.That(ids.Agent.LastDecision).Contains("lift");
        await Assert.That(sim.State.PendingCommands.OfType<PostHubOrder>().Any(o => o.Side == HubOrderSide.Buy)).IsTrue();
    }

    [Test]
    public async Task ActiveHaul_ResumesAfterCargoAvailable()
    {
        var (sim, ids) = CreateCarrierWorld(cargoAtNorth: 0m, minMargin: 0m);
        sim.Enqueue(new PostHubOrder(
            ids.Seller, ids.LocNorth, ids.Cargo, HubOrderSide.Sell, Quantity.From(10m), Money.From(2m)));
        sim.Enqueue(new PostHubOrder(
            ids.Buyer, ids.LocSouth, ids.Cargo, HubOrderSide.Buy, Quantity.From(10m), Money.From(20m)));
        await sim.AdvanceAsync(SimulationDuration.FromHours(1));

        ids.Agent.Tick(Ctx(sim, 113ul));
        await Assert.That(ids.Agent.LastDecision).Contains("lift");

        sim.State.World.Inventory.Add(
            new InventoryKey(ids.Carrier, ids.LocNorth, ids.Cargo),
            new ProductBatch(ids.Cargo, Quantity.From(10m), new ProductQuality(100m), Money.From(2m), SimulationDate.Epoch, null));

        ids.Agent.Tick(Ctx(sim, 114ul));
        await Assert.That(ids.Agent.LastDecision).Contains("haul");
    }

    private static AgentContext Ctx(EconomySimulation sim, ulong salt) =>
        new(sim, new DeterministicRandom(salt));

    private static (EconomySimulation Sim, CarrierIds Ids) CreateCarrierWorld(
        decimal cargoAtNorth = 0m,
        decimal minMargin = 0m,
        bool canOperate = true,
        bool avoidSouth = false,
        decimal fuelAtNorth = 20m,
        decimal fuelAtTransfer = 20m)
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
        var locTransfer = InventoryLocationId.From(builder.NextGuid());
        var locSouth = InventoryLocationId.From(builder.NextGuid());
        var hubNorth = TransportHubId.From(builder.NextGuid());
        var hubTransfer = TransportHubId.From(builder.NextGuid());
        var hubSouth = TransportHubId.From(builder.NextGuid());
        var vehicleId = VehicleClassId.From(builder.NextGuid());
        var cargoCat = ProductCategoryId.From(builder.NextGuid());
        var fuelCat = ProductCategoryId.From(builder.NextGuid());
        var cargo = ProductId.From(builder.NextGuid());
        var fuel = ProductId.From(builder.NextGuid());
        var process = ProductionProcessId.From(Guid.Parse("00000000-0000-4000-8000-000000000099"));

        var vehicle = new VehicleClass(
            vehicleId,
            CargoCapacity: Quantity.From(30m),
            FuelBurnPerDifficultyHour: 1m,
            CrewLaborPerUnderwayHour: 2m,
            FuelTankCapacity: Quantity.From(8m));

        builder
            .AddProduct(new ProductDefinition(
                cargo, cargoCat, ImmutableArray<ProductInput>.Empty,
                ImmutableArray<ProductAttributeDefinition>.Empty, process, null))
            .AddProduct(new ProductDefinition(
                fuel, fuelCat, ImmutableArray<ProductInput>.Empty,
                ImmutableArray<ProductAttributeDefinition>.Empty, process, null))
            .AddFirm(carrier, "MV Carrier", Money.From(10_000m))
            .AddFirm(seller, "Seller", Money.From(5_000m))
            .AddFirm(buyer, "Buyer", Money.From(5_000m))
            .AddHub(new TransportHub(hubNorth, locNorth, "North", DwellHours: 1, BerthCapacity: 2))
            .AddHub(new TransportHub(hubTransfer, locTransfer, "Transfer", DwellHours: 1, BerthCapacity: 2))
            .AddHub(new TransportHub(hubSouth, locSouth, "South", DwellHours: 1, BerthCapacity: 2))
            .AddCorridor(new TransportCorridor(
                TransportCorridorId.From(builder.NextGuid()), hubNorth, hubTransfer,
                TransitHours: 3, MaxCargo: Quantity.From(30m), Difficulty: 1m, Toll: Money.From(5m)))
            .AddCorridor(new TransportCorridor(
                TransportCorridorId.From(builder.NextGuid()), hubTransfer, hubSouth,
                TransitHours: 3, MaxCargo: Quantity.From(30m), Difficulty: 1m, Toll: Money.From(5m)))
            .AddCorridor(new TransportCorridor(
                TransportCorridorId.From(builder.NextGuid()), hubNorth, hubSouth,
                TransitHours: 3, MaxCargo: Quantity.From(30m), Difficulty: 1m, Toll: Money.From(5m)))
            .AddVehicleClass(vehicle)
            .SetTransportFuel(fuel, Money.From(1m))
            .SetLabor(carrier, 24m);

        if (fuelAtNorth > 0m)
        {
            builder.AddInventory(carrier, locNorth, new ProductBatch(
                fuel, Quantity.From(fuelAtNorth), new ProductQuality(100m), Money.From(1m), SimulationDate.Epoch, null));
        }

        if (fuelAtTransfer > 0m)
        {
            builder.AddInventory(carrier, locTransfer, new ProductBatch(
                fuel, Quantity.From(fuelAtTransfer), new ProductQuality(100m), Money.From(1m), SimulationDate.Epoch, null));
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
                MinMargin: minMargin,
                GatePrice: _ => 2m,
                FuelBuyLimitPrice: 2m,
                CanOperate: canOperate ? null : () => false,
                AvoidHub: avoidSouth ? hub => hub.Equals(hubSouth) : null),
            hubNorth);

        var ids = new CarrierIds(carrier, seller, buyer, locNorth, locSouth, hubNorth, hubSouth, vehicleId, cargo, fuel, agent);
        return (sim, ids);
    }

    private sealed record CarrierIds(
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
}
