using Novolis.Economy.Abstractions;
using Novolis.Economy.Core;
using Novolis.Economy.Core.Finance;
using Novolis.Economy.Core.Holdings;
using Novolis.Economy.Core.Invariants;
using Novolis.Economy.Core.Production;
using Novolis.Economy.Core.Transactions;
using Novolis.Economy.Primitives;

namespace Novolis.Economy.Models.DeterministicBounded;

/// <summary>
/// Small finite model used for exact regression and teaching. It has no
/// stochastic actors and no external-sector source or sink.
/// </summary>
public sealed class DeterministicBoundedModel : IEconomicModel
{
    private static readonly Guid RegionGuid =
        Guid.Parse("b1000000-0000-4000-8000-000000000001");
    private static readonly Guid ProducerGuid =
        Guid.Parse("b2000000-0000-4000-8000-000000000001");
    private static readonly Guid HouseholdGuid =
        Guid.Parse("b2000000-0000-4000-8000-000000000002");
    private static readonly Guid FoodGuid =
        Guid.Parse("b3000000-0000-4000-8000-000000000001");
    private static readonly Guid ActivityGuid =
        Guid.Parse("b5000000-0000-4000-8000-000000000001");

    private static readonly IReadOnlyList<RuleIdentity> SelectedRules =
    [
        new("production", "finite-capacity", "1"),
        new("markets", "posted-price-rationing", "1"),
        new("population", "fixed-cohort-demand", "1"),
        new("closure", "closed", "1")
    ];

    public DeterministicBoundedModel(
        DeterministicBoundedSpecification? specification = null)
    {
        Specification = specification ?? new DeterministicBoundedSpecification();
        ValidateSpecification(Specification);
    }

    public DeterministicBoundedSpecification Specification { get; }

    IEconomicModelSpecification IEconomicModel.Specification => Specification;

    public EconomicModelIdentity Identity =>
        new("DeterministicBounded", Specification.Version);

    public IReadOnlyList<RuleIdentity> Rules => SelectedRules;

    public IReadOnlyList<string> ActorProfileIds => ["agents.disabled"];

    public DeterministicBoundedState CreateState(
        DeterministicBoundedScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        return new(
            Identity,
            Tick: 0,
            CreateInitialAuthority(),
            scenario);
    }

    public IEconomicModelState CreateState(IEconomicScenario scenario) =>
        CreateState(scenario as DeterministicBoundedScenario
            ?? throw new ArgumentException(
                $"Expected {nameof(DeterministicBoundedScenario)}.",
                nameof(scenario)));

    public EconomicTickResult Advance(
        DeterministicBoundedState state,
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

        var authority = state.Authority with { SimulationSeed = context.Seed };
        var journalStart = authority.Journal.Count;
        var activity = authority.Activities.Values.Single();
        var runs = Math.Min(
            Specification.ProductionPerTick,
            ProductionCalculator.ActualRuns(authority, activity));
        if (runs > 0m)
        {
            authority = ProductionCalculator.ApplyRuns(authority, activity, runs);
            authority = authority with
            {
                Flows = authority.Flows.RecordProductionQuantity(
                    FoodAsset,
                    runs)
            };
        }

        var requested = Specification.HouseholdCount *
            Specification.DemandPerHouseholdPerTick;
        var available = HoldingLedger.GetQuantity(
            authority,
            Producer,
            Region,
            Food);
        var cash = CashLedger.Balance(authority, Household).Amount;
        var affordable = cash / Specification.FoodPrice;
        var sold = Math.Min(requested, Math.Min(available, affordable));
        if (sold > 0m)
        {
            var amount = sold * Specification.FoodPrice;
            authority = EconomicTransactionEngine.Apply(
                authority,
                EconomicTransaction.Create(
                    authority,
                    [
                        new PositionChange(
                            EconomicIdentity.For(Producer),
                            FoodAsset,
                            -sold,
                            Region),
                        new PositionChange(
                            EconomicIdentity.For(Household),
                            FoodAsset,
                            sold,
                            Region),
                        new PositionChange(
                            EconomicIdentity.For(Household),
                            authority.MonetaryAssetId,
                            -amount,
                            null),
                        new PositionChange(
                            EconomicIdentity.For(Producer),
                            authority.MonetaryAssetId,
                            amount,
                            null)
                    ],
                    "bounded-food-purchase",
                    "model-tick"));
            authority = authority with
            {
                Flows = authority.Flows.RecordConsumedQuantity(FoodAsset, sold)
            };
        }

        var unmet = Math.Max(0m, requested - sold);
        if (context.IsPeriodBoundary)
            authority = authority with { Period = checked(authority.Period + 1) };

        var next = state with
        {
            Tick = context.Tick,
            Authority = authority,
            Produced = state.Produced + runs,
            Sold = state.Sold + sold,
            UnmetDemand = state.UnmetDemand + unmet
        };

        return new EconomicTickResult(
            next,
            CreateReceipts(authority, journalStart, context.Tick),
            [
                new("food-produced", runs, "units"),
                new("food-sold", sold, "units"),
                new("food-unmet-demand", unmet, "units"),
                new(
                    "food-market-clearing-rate",
                    requested <= 0m ? 1m : sold / requested,
                    "ratio")
            ]);
    }

    public EconomicTickResult Advance(
        IEconomicModelState state,
        EconomicTickContext context) =>
        Advance(
            state as DeterministicBoundedState
                ?? throw new ArgumentException(
                    $"Expected {nameof(DeterministicBoundedState)}.",
                    nameof(state)),
            context);

    internal static ulong Fingerprint(DeterministicBoundedState state)
    {
        const ulong offset = 14695981039346656037UL;
        var hash = offset;
        Add(ref hash, state.Model.Id);
        Add(ref hash, state.Model.Version);
        Add(ref hash, state.Tick);
        Add(ref hash, state.Authority.Period);
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

        Add(ref hash, state.Produced);
        Add(ref hash, state.Sold);
        Add(ref hash, state.UnmetDemand);
        return hash;
    }

    private EconomyState CreateInitialAuthority()
    {
        var entities = new Dictionary<LegalEntityId, LegalEntity>
        {
            [Producer] = new(Producer, LegalEntityKind.Firm, Money.From(Specification.InitialCash)),
            [Household] = new(
                Household,
                LegalEntityKind.Household,
                Money.From(Specification.HouseholdCount * Specification.HouseholdCash))
        };
        var resources = new Dictionary<ResourceId, Resource>
        {
            [Food] = new(Food, "Food", ResourceKind.ConsumerGood, FoodAsset)
        };
        var cohorts = new Dictionary<CohortId, HouseholdCohort>
        {
            [Cohort] = new(
                Cohort,
                Region,
                Specification.HouseholdCount,
                new HouseholdProfile(1m, 0m, 1m, 0m),
                HouseholdLaborKind.Mean,
                Money.From(Specification.HouseholdCash),
                Household)
        };
        var activity = new Activity(
            ActivityId.From(ActivityGuid),
            Producer,
            Region,
            new ActivityRecipe(
                Array.Empty<ResourceAmount>(),
                [new ResourceAmount(Food, 1m)],
                LaborHoursPerRun: 1m,
                ProductionSpacePerRun: 1m),
            Specification.ProductionPerTick);
        var positions = new Dictionary<string, EconomicPosition>();
        SetPosition(
            positions,
            EconomicIdentity.For(Producer),
            null,
            EconomyState.DefaultUnitOfAccountAssetId,
            Specification.InitialCash);
        SetPosition(
            positions,
            EconomicIdentity.For(Household),
            null,
            EconomyState.DefaultUnitOfAccountAssetId,
            Specification.HouseholdCount * Specification.HouseholdCash);
        SetPosition(
            positions,
            EconomicIdentity.For(Producer),
            Region,
            FoodAsset,
            Specification.InitialFood);

        return EconomyState.Empty with
        {
            Entities = entities,
            Regions = new Dictionary<RegionId, Region>
            {
                [Region] = new(Region, 100, 100m, 100m)
            },
            Cohorts = cohorts,
            Activities = new Dictionary<ActivityId, Activity>
            {
                [activity.Id] = activity
            },
            Resources = resources,
            Positions = positions,
            Policy = StatePolicy.Neutral
        };
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
        DeterministicBoundedSpecification specification)
    {
        if (specification.HouseholdCount <= 0 ||
            specification.InitialFood < 0m ||
            specification.InitialCash < 0m ||
            specification.HouseholdCash < 0m ||
            specification.ProductionPerTick < 0m ||
            specification.DemandPerHouseholdPerTick < 0m ||
            specification.FoodPrice <= 0m)
        {
            throw new ArgumentException(
                "Bounded model quantities must be non-negative and price positive.",
                nameof(specification));
        }
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

    private static RegionId Region => RegionId.From(RegionGuid);
    private static ResourceId Food => ResourceId.From(FoodGuid);
    private static EconomicAssetId FoodAsset => EconomicAssetId.From(FoodGuid);
    private static LegalEntityId Producer => LegalEntityId.From(ProducerGuid);
    private static LegalEntityId Household => LegalEntityId.From(HouseholdGuid);
    private static CohortId Cohort => CohortId.From(HouseholdGuid);
}
