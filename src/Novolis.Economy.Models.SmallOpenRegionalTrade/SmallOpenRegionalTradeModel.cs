using Novolis.Economy.Abstractions;
using Novolis.Economy.Core;
using Novolis.Economy.Core.Finance;
using Novolis.Economy.Core.Holdings;
using Novolis.Economy.Core.Invariants;
using Novolis.Economy.Core.Production;
using Novolis.Economy.Core.Transactions;
using Novolis.Economy.Primitives;

namespace Novolis.Economy.Models.SmallOpenRegionalTrade;

/// <summary>Structural parameters for the small open regional trade model.</summary>
public sealed record SmallOpenRegionalTradeSpecification(
    string Version = "small-open-regional-trade-3",
    decimal FoodPrice = 10m,
    decimal FuelPrice = 10m,
    decimal ImportCapacityPerTick = 12m,
    decimal ExportDemandPerTick = 8m,
    decimal HouseholdFoodDemandPerTick = 1m,
    decimal FoodProductionCapacityPerTick = 12m,
    int PeriodLengthTicks = 24,
    decimal CreditLimit = 10_000m,
    decimal CreditInterestRatePerPeriod = 0.01m,
    int CreditTermPeriods = 4,
    MonetaryClosure Closure = MonetaryClosure.ExternalSector,
    FoodMarketMechanism MarketMechanism = FoodMarketMechanism.PostedPriceRationing,
    FoodDemandMechanism DemandMechanism = FoodDemandMechanism.CohortBudgetShare,
    CreditMechanism CreditMechanism = CreditMechanism.BoundedWorkingCapital,
    InterestConvention InterestConvention = InterestConvention.CompoundPerPeriod,
    DefaultResolution DefaultResolution = DefaultResolution.KeepOutstanding,
    TaxBase TaxBase = TaxBase.None,
    decimal TaxRate = 0m,
    decimal PriceAdjustmentRate = 0.5m,
    decimal MinimumFoodPrice = 1m,
    decimal MaximumFoodPrice = 100m,
    decimal SubsistenceFoodPerHouseholdPerTick = 0.5m,
    decimal DiscretionaryFoodShare = 0.5m)
    : IEconomicModelSpecification;

/// <summary>Named initial conditions for the flagship model.</summary>
public sealed record SmallOpenRegionalTradeScenario(
    string Id = "three-region-baseline",
    string Version = "small-open-regional-trade-scenario-1",
    int RegionCount = 3,
    int HouseholdCount = 120,
    decimal HouseholdOpeningCash = 100m,
    decimal ProducerOpeningCash = 1_000m,
    decimal ExternalOpeningCash = 100_000m,
    decimal ExternalFuelStock = 10_000m,
    decimal BankOpeningCash = 50_000m) : IEconomicScenario
{
    /// <summary>Canonical scenario used by examples and integration tests.</summary>
    public static SmallOpenRegionalTradeScenario Baseline { get; } = new();
}

/// <summary>Player or actor request for additional household food demand.</summary>
public sealed record PurchaseFoodCommand(decimal Quantity) : IEconomicModelCommand
{
    public string Kind => "purchase-food";
}

/// <summary>Request for a bounded working-capital draw from the model bank.</summary>
public sealed record DrawWorkingCapitalCommand(decimal Amount) : IEconomicModelCommand
{
    public string Kind => "draw-working-capital";
}

/// <summary>Opaque state returned by the flagship model.</summary>
public sealed record SmallOpenRegionalTradeState(
    EconomicModelIdentity Model,
    long Tick,
    EconomyState Authority,
    SmallOpenRegionalTradeScenario Scenario,
    decimal ImportedFuel = 0m,
    decimal ExportedFood = 0m,
    decimal ProducedFood = 0m,
    decimal SoldFood = 0m,
    decimal UnmetFoodDemand = 0m) : IEconomicModelState, IEconomicStateValidation
{
    /// <summary>Core authority state for Accounting and diagnostics.</summary>
    public EconomyState CoreState => Authority;

    /// <summary>Stable fingerprint of model state and authoritative positions.</summary>
    public ulong Fingerprint => SmallOpenRegionalTradeModel.Fingerprint(this);

    IReadOnlyList<EconomicValidationSignal>
        IEconomicStateValidation.ValidateState() =>
        InvariantChecker.Check(Authority)
            .Select(violation => new EconomicValidationSignal(
                violation.Code,
                Passed: false,
                violation.Message))
            .ToList();
}

/// <summary>
/// Host-neutral flagship model. It owns the meaning of one economic tick;
/// a game clock, Simulation runner, or research harness may drive it.
/// </summary>
public sealed class SmallOpenRegionalTradeModel : IEconomicModel
{
    private static readonly Guid NorthGuid =
        Guid.Parse("1d1e1f20-0000-4000-8000-000000000001");
    private static readonly Guid CentralGuid =
        Guid.Parse("1d1e1f20-0000-4000-8000-000000000002");
    private static readonly Guid SouthGuid =
        Guid.Parse("1d1e1f20-0000-4000-8000-000000000003");
    private static readonly Guid FoodGuid =
        Guid.Parse("2d2e2f30-0000-4000-8000-000000000001");
    private static readonly Guid FuelGuid =
        Guid.Parse("2d2e2f30-0000-4000-8000-000000000002");
    private static readonly Guid HouseholdGuid =
        Guid.Parse("3d3e3f40-0000-4000-8000-000000000001");
    private static readonly Guid ProducerGuid =
        Guid.Parse("3d3e3f40-0000-4000-8000-000000000002");
    private static readonly Guid ExternalGuid =
        Guid.Parse("3d3e3f40-0000-4000-8000-000000000005");
    private static readonly Guid BankGuid =
        Guid.Parse("3d3e3f40-0000-4000-8000-000000000006");
    private static readonly Guid ActivityGuid =
        Guid.Parse("4d4e4f50-0000-4000-8000-000000000001");
    private static readonly Guid FacilityGuid =
        Guid.Parse("5d5e5f60-0000-4000-8000-000000000001");
    private static readonly Guid CreditFacilityGuid =
        Guid.Parse("6d6e6f70-0000-4000-8000-000000000001");

    private static readonly IReadOnlyList<string> SelectedActors =
    [
        "firms.rule-based",
        "households.cohort-aggregate"
    ];

    /// <summary>Creates the flagship model with structural defaults.</summary>
    public SmallOpenRegionalTradeModel(
        SmallOpenRegionalTradeSpecification? specification = null)
    {
        Specification = specification ?? new SmallOpenRegionalTradeSpecification();
        ValidateSpecification(Specification);
    }

    /// <summary>Structural assumptions selected by this model.</summary>
    public SmallOpenRegionalTradeSpecification Specification { get; }

    IEconomicModelSpecification IEconomicModel.Specification => Specification;

    /// <inheritdoc />
    public EconomicModelIdentity Identity =>
        new("SmallOpenRegionalTrade", Specification.Version);

    /// <inheritdoc />
    public IReadOnlyList<RuleIdentity> Rules =>
    [
        new("production", "capacity-constrained", "1"),
        new(
            "population",
            Specification.DemandMechanism == FoodDemandMechanism.CohortBudgetShare
                ? "cohort-budget-share"
                : "linear-expenditure",
            "1"),
        new(
            "markets",
            Specification.MarketMechanism == FoodMarketMechanism.PostedPriceRationing
                ? "posted-price-rationing"
                : "supply-demand-price-discovery",
            "1"),
        new(
            "finance",
            Specification.CreditMechanism == CreditMechanism.Disabled
                ? "disabled"
                : "working-capital-credit",
            "1"),
        new("logistics", "regional-settlement", "1"),
        new(
            "closure",
            Specification.Closure == MonetaryClosure.ExternalSector
                ? "explicit-external-sector"
                : Specification.Closure.ToString().ToLowerInvariant(),
            "1")
    ];

    /// <inheritdoc />
    public IReadOnlyList<string> ActorProfileIds => SelectedActors;

    /// <summary>Creates the model state for a typed scenario.</summary>
    public SmallOpenRegionalTradeState CreateState(
        SmallOpenRegionalTradeScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ValidateScenario(scenario);
        return new(
            Identity,
            Tick: 0,
            CreateInitialAuthority(scenario),
            scenario);
    }

    /// <inheritdoc />
    public IEconomicModelState CreateState(IEconomicScenario scenario) =>
        CreateState(scenario as SmallOpenRegionalTradeScenario
            ?? throw new ArgumentException(
                $"Expected {nameof(SmallOpenRegionalTradeScenario)}.",
                nameof(scenario)));

    /// <summary>Advances a typed state by exactly one host-provided tick.</summary>
    public EconomicTickResult Advance(
        SmallOpenRegionalTradeState state,
        EconomicTickContext context)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(context);
        if (context.Tick != state.Tick + 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(context),
                context.Tick,
                $"Expected tick {state.Tick + 1}.");
        }
        if (context.Commands.Any(command =>
                command is PurchaseFoodCommand purchase && purchase.Quantity < 0m ||
                command is DrawWorkingCapitalCommand draw && draw.Amount < 0m))
        {
            throw new ArgumentOutOfRangeException(
                nameof(context),
                "Model commands cannot request negative quantities or balances.");
        }

        var authority = state.Authority with
        {
            SimulationSeed = context.Seed
        };
        var journalStart = authority.Journal.Count;
        var importedFuel = 0m;
        var exportedFood = 0m;
        var producedFood = 0m;
        var soldFood = 0m;

        foreach (var activity in authority.Activities.Values
                     .OrderBy(activity => activity.Id.Value))
        {
            var runs = ProductionCalculator.ActualRuns(authority, activity);
            if (runs <= 0m)
                continue;

            authority = ProductionCalculator.ApplyRuns(authority, activity, runs);
            var output = activity.Recipe.Outputs
                .Where(output => output.ResourceId.Equals(FoodResource))
                .Sum(output => output.Quantity * runs);
            producedFood += output;
            authority = authority with
            {
                Flows = authority.Flows.RecordProductionQuantity(
                    FoodAsset,
                    output)
            };
        }

        if (Specification.Closure != MonetaryClosure.Closed)
        {
            authority = ApplyCreditCommands(authority, context);
            (authority, importedFuel) = ImportFuel(authority);
            (authority, exportedFood) = ExportFood(authority);
        }
        else
        {
            authority = ApplyCreditCommands(authority, context);
        }

        var requestedFood = RequestedFood(state, context);
        var foodPrice = FoodPrice(authority, requestedFood);
        (authority, soldFood) = SellFood(authority, requestedFood, foodPrice);
        var unmetFoodDemand = Math.Max(0m, requestedFood - soldFood);

        if (context.IsPeriodBoundary)
        {
            authority = AccruePeriodInterest(authority);
            authority = authority with { Period = checked(authority.Period + 1) };
        }

        var nextState = state with
        {
            Tick = context.Tick,
            Authority = authority,
            ImportedFuel = state.ImportedFuel + importedFuel,
            ExportedFood = state.ExportedFood + exportedFood,
            ProducedFood = state.ProducedFood + producedFood,
            SoldFood = state.SoldFood + soldFood,
            UnmetFoodDemand = state.UnmetFoodDemand + unmetFoodDemand
        };

        var receipts = CreateReceipts(
            authority,
            journalStart,
            context.Tick);
        return new EconomicTickResult(
            nextState,
            receipts,
            [
                new("food-produced", producedFood, "units"),
                new("food-sold", soldFood, "units"),
                new("food-unmet-demand", unmetFoodDemand, "units"),
                new("fuel-imported", importedFuel, "units"),
                new("food-exported", exportedFood, "units"),
                new(
                    "food-market-clearing-rate",
                    requestedFood <= 0m ? 1m : soldFood / requestedFood,
                    "ratio",
                    "Quantity sold divided by quantity requested."),
                new(
                    "food-price",
                    foodPrice,
                    "unit-of-account/unit",
                    "Transaction price used for household food purchases."),
                new(
                    "producer-debt",
                    ClaimLedger.Snapshot(authority).Values
                        .Where(claim => claim.Debtor == EconomicIdentity.For(ProducerEntity))
                        .Sum(claim => claim.Principal.Quantity),
                    "unit-of-account")
            ]);
    }

    /// <inheritdoc />
    public EconomicTickResult Advance(
        IEconomicModelState state,
        EconomicTickContext context) =>
        Advance(
            state as SmallOpenRegionalTradeState
                ?? throw new ArgumentException(
                    $"Expected {nameof(SmallOpenRegionalTradeState)}.",
                    nameof(state)),
            context);

    private (EconomyState State, decimal Quantity) ImportFuel(
        EconomyState state)
    {
        var producer = ProducerEntity;
        var external = ExternalEntity;
        var region = NorthRegion;
        var externalStock = HoldingLedger.GetQuantity(
            state,
            external,
            region,
            FuelResource);
        var producerCash = CashLedger.Balance(state, producer).Amount;
        var affordable = Specification.FuelPrice <= 0m
            ? 0m
            : producerCash / Specification.FuelPrice;
        var quantity = Math.Min(
            Specification.ImportCapacityPerTick,
            Math.Min(externalStock, Math.Max(0m, affordable)));
        if (quantity <= 0m)
            return (state, 0m);

        var amount = quantity * Specification.FuelPrice;
        state = ApplyTransaction(
            state,
            "regional-import",
            [
                new PositionChange(
                    EconomicIdentity.For(external),
                    FuelAsset,
                    -quantity,
                    region),
                new PositionChange(
                    EconomicIdentity.For(producer),
                    FuelAsset,
                    quantity,
                    region),
                new PositionChange(
                    EconomicIdentity.For(producer),
                    state.MonetaryAssetId,
                    -amount,
                    null),
                new PositionChange(
                    EconomicIdentity.For(external),
                    state.MonetaryAssetId,
                    amount,
                    null)
            ]);
        return (state, quantity);
    }

    private EconomyState ApplyCreditCommands(
        EconomyState state,
        EconomicTickContext context)
    {
        if (Specification.CreditMechanism == CreditMechanism.Disabled)
        {
            return state;
        }

        var requested = context.Commands
            .OfType<DrawWorkingCapitalCommand>()
            .Where(command => command.Amount > 0m)
            .Sum(command => command.Amount);
        if (requested <= 0m)
            return state;

        var facility = state.CreditFacilities.Values
            .SingleOrDefault(item =>
                item.Provider == BankEntity &&
                item.Borrower == ProducerEntity &&
                item.IsCommitted);
        if (facility is null)
            return state;

        var amount = Math.Min(
            requested,
            Math.Min(
                facility.Available.Amount,
                CashLedger.Balance(state, BankEntity).Amount));
        if (amount <= 0m)
            return state;

        var claim = new FinancialClaim(
            ClaimId.From(DeterministicIds.GuidFor(
                "small-open-working-capital",
                state.SimulationSeed,
                state.Period,
                state.TransitionSequence,
                amount)),
            EconomicIdentity.For(BankEntity),
            EconomicIdentity.For(ProducerEntity),
            new AssetAmount(state.MonetaryAssetId, amount),
            Specification.CreditInterestRatePerPeriod,
            Specification.CreditTermPeriods,
            LoanStatus.Performing);
        state = ApplyTransaction(
            state,
            "working-capital-origination",
            [
                new PositionChange(
                    EconomicIdentity.For(BankEntity),
                    state.MonetaryAssetId,
                    -amount,
                    null),
                new PositionChange(
                    EconomicIdentity.For(ProducerEntity),
                    state.MonetaryAssetId,
                    amount,
                    null),
                new CreateClaim(claim)
            ]);
        var facilities = new Dictionary<CreditFacilityId, CreditFacility>(
            state.CreditFacilities)
        {
            [facility.Id] = facility with
            {
                Drawn = facility.Drawn + Money.From(amount)
            }
        };
        return state with { CreditFacilities = facilities };
    }

    private EconomyState AccruePeriodInterest(EconomyState state)
    {
        var claims = ClaimLedger.Snapshot(state).Values
            .Where(claim =>
                claim.Debtor == EconomicIdentity.For(ProducerEntity) &&
                claim.Status is LoanStatus.Performing or LoanStatus.Delinquent &&
                claim.InterestRatePerPeriod > 0m)
            .OrderBy(claim => claim.Id.Value)
            .ToList();
        foreach (var claim in claims)
        {
            var interestBase =
                Specification.InterestConvention ==
                    InterestConvention.SimplePerPeriod
                    ? (claim.OriginalPrincipal?.Quantity ??
                       claim.Principal.Quantity)
                    : claim.Principal.Quantity;
            var interest = interestBase * claim.InterestRatePerPeriod;
            if (interest <= 0m)
                continue;

            var nextRemainingPeriods = Math.Max(0, claim.RemainingPeriods - 1);
            var nextStatus = claim.Status;
            var nextPrincipal = claim.Principal.Quantity + interest;
            if (nextRemainingPeriods == 0)
            {
                switch (Specification.DefaultResolution)
                {
                    case DefaultResolution.MarkDefaultAtMaturity:
                        nextStatus = LoanStatus.Defaulted;
                        break;
                    case DefaultResolution.WriteOffAtMaturity:
                        nextStatus = LoanStatus.Defaulted;
                        nextPrincipal = 0m;
                        break;
                }
            }

            state = ApplyTransaction(
                state,
                "working-capital-interest-accrual",
                [
                    new CreateClaim(claim with
                    {
                        Principal = new AssetAmount(
                            claim.Principal.Asset,
                            nextPrincipal),
                        AccruedInterest = claim.AccruedInterest + interest,
                        OriginalPrincipal = claim.OriginalPrincipal ??
                            claim.Principal,
                        RemainingPeriods = nextRemainingPeriods,
                        Status = nextStatus
                    })
                ]);
        }

        return state;
    }

    private (EconomyState State, decimal Quantity) ExportFood(
        EconomyState state)
    {
        var producer = ProducerEntity;
        var external = ExternalEntity;
        var region = NorthRegion;
        var stock = HoldingLedger.GetQuantity(
            state,
            producer,
            region,
            FoodResource);
        var externalCash = CashLedger.Balance(state, external).Amount;
        var affordable = Specification.FoodPrice <= 0m
            ? 0m
            : externalCash / Specification.FoodPrice;
        var quantity = Math.Min(
            Specification.ExportDemandPerTick,
            Math.Min(stock, Math.Max(0m, affordable)));
        if (quantity <= 0m)
            return (state, 0m);

        var amount = quantity * Specification.FoodPrice;
        state = ApplyTransaction(
            state,
            "regional-export",
            [
                new PositionChange(
                    EconomicIdentity.For(producer),
                    FoodAsset,
                    -quantity,
                    region),
                new PositionChange(
                    EconomicIdentity.For(external),
                    FoodAsset,
                    quantity,
                    region),
                new PositionChange(
                    EconomicIdentity.For(producer),
                    state.MonetaryAssetId,
                    amount,
                    null),
                new PositionChange(
                    EconomicIdentity.For(external),
                    state.MonetaryAssetId,
                    -amount,
                    null)
            ]);
        return (state, quantity);
    }

    private decimal RequestedFood(
        SmallOpenRegionalTradeState state,
        EconomicTickContext context)
    {
        var baseDemand = state.Scenario.HouseholdCount *
            Specification.HouseholdFoodDemandPerTick;
        if (Specification.DemandMechanism == FoodDemandMechanism.LinearExpenditure)
        {
            var cash = CashLedger.Balance(
                state.Authority,
                HouseholdEntity).Amount;
            var subsistence = state.Scenario.HouseholdCount *
                Math.Min(
                    Specification.SubsistenceFoodPerHouseholdPerTick,
                    Specification.HouseholdFoodDemandPerTick);
            var affordableEnvelope = Specification.FoodPrice <= 0m
                ? 0m
                : Math.Max(0m, cash / Specification.FoodPrice);
            baseDemand = subsistence +
                Math.Max(0m, affordableEnvelope - subsistence) *
                Specification.DiscretionaryFoodShare;
        }

        return baseDemand +
            context.Commands
                .OfType<PurchaseFoodCommand>()
                .Where(command => command.Quantity > 0m)
                .Sum(command => command.Quantity);
    }

    private decimal FoodPrice(EconomyState state, decimal requested)
    {
        if (Specification.MarketMechanism ==
            FoodMarketMechanism.PostedPriceRationing)
        {
            return Specification.FoodPrice;
        }

        var stock = HoldingLedger.GetQuantity(
            state,
            ProducerEntity,
            NorthRegion,
            FoodResource);
        if (stock <= 0m)
        {
            return Specification.MaximumFoodPrice;
        }

        var pressure = requested / stock;
        var undiscounted = Specification.FoodPrice *
            (1m + Specification.PriceAdjustmentRate * (pressure - 1m));
        return Math.Clamp(
            undiscounted,
            Specification.MinimumFoodPrice,
            Specification.MaximumFoodPrice);
    }

    private (EconomyState State, decimal Quantity) SellFood(
        EconomyState state,
        decimal requested,
        decimal price)
    {
        if (requested <= 0m)
            return (state, 0m);

        var producer = ProducerEntity;
        var household = HouseholdEntity;
        var region = NorthRegion;
        var stock = HoldingLedger.GetQuantity(
            state,
            producer,
            region,
            FoodResource);
        var householdCash = CashLedger.Balance(state, household).Amount;
        var affordable = price <= 0m
            ? 0m
            : householdCash / price;
        var quantity = Math.Min(
            requested,
            Math.Min(stock, Math.Max(0m, affordable)));
        if (quantity <= 0m)
            return (state, 0m);

        var amount = quantity * price;
        state = ApplyTransaction(
            state,
            "household-food-purchase",
            [
                new PositionChange(
                    EconomicIdentity.For(producer),
                    FoodAsset,
                    -quantity,
                    region),
                new PositionChange(
                    EconomicIdentity.For(household),
                    FoodAsset,
                    quantity,
                    region),
                new PositionChange(
                    EconomicIdentity.For(household),
                    state.MonetaryAssetId,
                    -amount,
                    null),
                new PositionChange(
                    EconomicIdentity.For(producer),
                    state.MonetaryAssetId,
                    amount,
                    null)
            ]);
        state = state with
        {
            Flows = state.Flows.RecordConsumedQuantity(FoodAsset, quantity)
        };
        return (state, quantity);
    }

    private static EconomyState ApplyTransaction(
        EconomyState state,
        string reason,
        IReadOnlyList<EconomicEffect> effects)
    {
        return EconomicTransactionEngine.Apply(
            state,
            EconomicTransaction.Create(state, effects, reason, "model-tick"));
    }

    private static IReadOnlyList<EconomicTransitionReceipt> CreateReceipts(
        EconomyState state,
        int journalStart,
        long tick)
    {
        return state.Journal
            .Skip(journalStart)
            .Select(transaction => new EconomicTransitionReceipt(
                transaction.Id,
                transaction.Reason ?? "unspecified",
                transaction.Period ?? state.Period,
                tick,
                transaction.Effects
                    .Select(effect => effect switch
                    {
                        PositionChange change => new EconomicEffectRequest(
                            null,
                            "position-change",
                            change.Owner,
                            change.Asset,
                            change.Delta,
                            change.Region),
                        CreateClaim create => new EconomicEffectRequest(
                            create.Claim.Creditor,
                            "claim-created",
                            create.Claim.Debtor,
                            create.Claim.Principal.Asset,
                            create.Claim.Principal.Quantity),
                        SettleClaim settle => new EconomicEffectRequest(
                            null,
                            "claim-settled",
                            Asset: settle.Amount.Asset,
                            Quantity: -settle.Amount.Quantity),
                        _ => new EconomicEffectRequest(null, effect.GetType().Name)
                    })
                    .ToList()))
            .ToList();
    }

    private EconomyState CreateInitialAuthority(
        SmallOpenRegionalTradeScenario scenario)
    {
        var regions = CreateRegions(scenario.RegionCount);
        var entities = new Dictionary<LegalEntityId, LegalEntity>
        {
            [HouseholdEntity] = new(
                HouseholdEntity,
                LegalEntityKind.Household,
                Money.From(scenario.HouseholdCount * scenario.HouseholdOpeningCash)),
            [ProducerEntity] = new(
                ProducerEntity,
                LegalEntityKind.Firm,
                Money.From(scenario.ProducerOpeningCash)),
            [ExternalEntity] = new(
                ExternalEntity,
                LegalEntityKind.ExternalSector,
                Money.From(scenario.ExternalOpeningCash)),
            [BankEntity] = new(
                BankEntity,
                LegalEntityKind.Bank,
                Money.From(scenario.BankOpeningCash))
        };
        var resources = new Dictionary<ResourceId, Resource>
        {
            [FoodResource] = new(
                FoodResource,
                "Food",
                ResourceKind.ConsumerGood,
                FoodAsset),
            [FuelResource] = new(
                FuelResource,
                "Fuel",
                ResourceKind.IntermediateGood,
                FuelAsset)
        };
        var activity = new Activity(
            ActivityId.From(ActivityGuid),
            ProducerEntity,
            NorthRegion,
            new ActivityRecipe(
                Array.Empty<ResourceAmount>(),
                [new ResourceAmount(FoodResource, 1m)],
                LaborHoursPerRun: 1m,
                ProductionSpacePerRun: 1m),
            Specification.FoodProductionCapacityPerTick);
        var cohorts = new Dictionary<CohortId, HouseholdCohort>
        {
            [HouseholdCohortId] = new(
                HouseholdCohortId,
                NorthRegion,
                scenario.HouseholdCount,
                new HouseholdProfile(
                    ConsumptionWeight: 1m,
                    SavingsPreference: 0m,
                    LaborQuality: 1m,
                    MigrationPreference: 0m),
                HouseholdLaborKind.Mean,
                Money.From(scenario.HouseholdOpeningCash),
                HouseholdEntity)
        };
        var positions = new Dictionary<string, EconomicPosition>();
        SetPosition(
            positions,
            EconomicIdentity.For(HouseholdEntity),
            null,
            EconomyState.DefaultUnitOfAccountAssetId,
            scenario.HouseholdCount * scenario.HouseholdOpeningCash);
        SetPosition(
            positions,
            EconomicIdentity.For(ProducerEntity),
            null,
            EconomyState.DefaultUnitOfAccountAssetId,
            scenario.ProducerOpeningCash);
        SetPosition(
            positions,
            EconomicIdentity.For(ExternalEntity),
            null,
            EconomyState.DefaultUnitOfAccountAssetId,
            scenario.ExternalOpeningCash);
        SetPosition(
            positions,
            EconomicIdentity.For(BankEntity),
            null,
            EconomyState.DefaultUnitOfAccountAssetId,
            scenario.BankOpeningCash);
        SetPosition(
            positions,
            EconomicIdentity.For(ExternalEntity),
            NorthRegion,
            FuelAsset,
            scenario.ExternalFuelStock);

        var facilities = new Dictionary<CreditFacilityId, CreditFacility>
        {
            [CreditFacilityId.From(CreditFacilityGuid)] = new(
                CreditFacilityId.From(CreditFacilityGuid),
                BankEntity,
                ProducerEntity,
                Money.From(Specification.CreditLimit),
                Money.Zero,
                IsCommitted: true)
        };
        return EconomyState.Empty with
        {
            Entities = entities,
            Regions = regions,
            Cohorts = cohorts,
            Activities = new Dictionary<ActivityId, Activity>
            {
                [activity.Id] = activity
            },
            Resources = resources,
            CreditFacilities = facilities,
            Positions = positions,
            Policy = StatePolicy.Neutral with
            {
                WagePerLaborHour = Money.From(10m)
            }
        };
    }

    private Dictionary<RegionId, Region> CreateRegions(int count)
    {
        var ids = new[] { NorthRegion, CentralRegion, SouthRegion };
        return ids.Take(count)
            .ToDictionary(
                id => id,
                id => new Region(
                    id,
                    LivingCapacity: 1_000_000,
                    ProductionCapacity: Specification.FoodProductionCapacityPerTick,
                    LogisticsCapacity: 1_000_000));
    }

    private static void SetPosition(
        IDictionary<string, EconomicPosition> positions,
        EconomicEntityId owner,
        RegionId? region,
        EconomicAssetId asset,
        decimal quantity)
    {
        if (quantity <= 0m)
            return;
        var position = new EconomicPosition(owner, asset, quantity, region);
        positions[PositionLedger.Key(owner, region, asset)] = position;
    }

    private static void ValidateSpecification(
        SmallOpenRegionalTradeSpecification specification)
    {
        if (specification.FoodPrice <= 0m ||
            specification.FuelPrice <= 0m ||
            specification.PeriodLengthTicks <= 0 ||
            specification.FoodProductionCapacityPerTick < 0m ||
            specification.ImportCapacityPerTick < 0m ||
            specification.ExportDemandPerTick < 0m ||
            specification.HouseholdFoodDemandPerTick < 0m ||
            specification.CreditLimit < 0m ||
            specification.CreditInterestRatePerPeriod < 0m ||
            specification.CreditTermPeriods < 0 ||
            specification.TaxRate < 0m ||
            specification.TaxRate > 1m ||
            specification.PriceAdjustmentRate < 0m ||
            specification.MinimumFoodPrice <= 0m ||
            specification.MaximumFoodPrice < specification.MinimumFoodPrice ||
            specification.SubsistenceFoodPerHouseholdPerTick < 0m ||
            specification.DiscretionaryFoodShare < 0m ||
            specification.DiscretionaryFoodShare > 1m)
        {
            throw new ArgumentException(
                "Prices, period length, and capacities must be non-negative and prices positive.",
                nameof(specification));
        }
    }

    private static void ValidateScenario(SmallOpenRegionalTradeScenario scenario)
    {
        if (scenario.RegionCount is < 1 or > 3)
            throw new ArgumentOutOfRangeException(
                nameof(scenario),
                scenario.RegionCount,
                "The baseline scenario supports one to three regions.");
        if (scenario.HouseholdCount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(scenario),
                scenario.HouseholdCount,
                "At least one household is required.");
        if (scenario.HouseholdOpeningCash < 0m ||
            scenario.ProducerOpeningCash < 0m ||
            scenario.ExternalOpeningCash < 0m ||
            scenario.ExternalFuelStock < 0m ||
            scenario.BankOpeningCash < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(scenario),
                "Opening stocks and balances cannot be negative.");
        }
    }

    internal static ulong Fingerprint(SmallOpenRegionalTradeState state)
    {
        const ulong offset = 14695981039346656037UL;
        var hash = offset;
        Add(ref hash, state.Model.Id);
        Add(ref hash, state.Model.Version);
        Add(ref hash, state.Tick);
        Add(ref hash, state.Authority.Period);
        Add(ref hash, state.Authority.TransitionSequence);
        foreach (var position in state.Authority.PositionState
                     .OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            Add(ref hash, position.Key);
            Add(ref hash, position.Value.Quantity);
        }

        foreach (var claim in state.Authority.ClaimState.Values
                     .OrderBy(item => item.Id.Value))
        {
            Add(ref hash, claim.Id.ToString());
            Add(ref hash, claim.Creditor.ToString());
            Add(ref hash, claim.Debtor.ToString());
            Add(ref hash, claim.Principal.Asset.ToString());
            Add(ref hash, claim.Principal.Quantity);
            Add(ref hash, claim.InterestRatePerPeriod);
            Add(ref hash, claim.RemainingPeriods);
            Add(ref hash, claim.Status.ToString());
            Add(ref hash, claim.AccruedInterest);
            Add(ref hash, claim.OriginalPrincipal?.Quantity ?? -1m);
        }

        foreach (var transaction in state.Authority.Journal)
        {
            Add(ref hash, transaction.Id.ToString());
            Add(ref hash, transaction.Reason ?? string.Empty);
            Add(ref hash, transaction.Period ?? -1);
            foreach (var effect in transaction.Effects)
                Add(ref hash, effect.ToString() ?? string.Empty);
        }

        Add(ref hash, state.ImportedFuel);
        Add(ref hash, state.ExportedFood);
        Add(ref hash, state.ProducedFood);
        Add(ref hash, state.SoldFood);
        Add(ref hash, state.UnmetFoodDemand);
        return hash;
    }

    private static void Add(ref ulong hash, string value)
    {
        const ulong prime = 1099511628211UL;
        foreach (var valueByte in System.Text.Encoding.UTF8.GetBytes(value))
            hash = (hash ^ valueByte) * prime;
    }

    private static void Add(ref ulong hash, long value) =>
        Add(ref hash, value.ToString(System.Globalization.CultureInfo.InvariantCulture));

    private static void Add(ref ulong hash, int value) =>
        Add(ref hash, value.ToString(System.Globalization.CultureInfo.InvariantCulture));

    private static void Add(ref ulong hash, decimal value) =>
        Add(ref hash, value.ToString("G29", System.Globalization.CultureInfo.InvariantCulture));

    private static RegionId NorthRegion => RegionId.From(NorthGuid);
    private static RegionId CentralRegion => RegionId.From(CentralGuid);
    private static RegionId SouthRegion => RegionId.From(SouthGuid);
    private static ResourceId FoodResource => ResourceId.From(FoodGuid);
    private static ResourceId FuelResource => ResourceId.From(FuelGuid);
    private static EconomicAssetId FoodAsset => EconomicAssetId.From(FoodGuid);
    private static EconomicAssetId FuelAsset => EconomicAssetId.From(FuelGuid);
    private static LegalEntityId HouseholdEntity => LegalEntityId.From(HouseholdGuid);
    private static LegalEntityId ProducerEntity => LegalEntityId.From(ProducerGuid);
    private static LegalEntityId ExternalEntity => LegalEntityId.From(ExternalGuid);
    private static LegalEntityId BankEntity => LegalEntityId.From(BankGuid);
    private static CohortId HouseholdCohortId => CohortId.From(HouseholdGuid);
}
