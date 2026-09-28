using System.Text.Json;
using Novolis.Economy.Core;
using Novolis.Economy.Core.Finance;
using Novolis.Economy.Core.Holdings;
using Novolis.Economy.Core.Transactions;
using Novolis.Economy.Simulation;
using CoreEntity = Novolis.Economy.Core.LegalEntity;
using CoreEntityKind = Novolis.Economy.Core.LegalEntityKind;

namespace Novolis.Economy.Unit.Core;

public sealed class EconomicGrammarTests
{
    private static readonly LegalEntityId Firm =
        LegalEntityId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

    private static readonly LegalEntityId Household =
        LegalEntityId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

    private static readonly RegionId Region =
        RegionId.From(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));

    private static readonly ResourceId Iron =
        ResourceId.From(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"));

    private static readonly EconomicAssetId IronAsset =
        EconomicAssetId.From(Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"));

    private static EconomyState StateWithCatalog() =>
        EconomyState.Empty with
        {
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [Firm] = new CoreEntity(Firm, CoreEntityKind.Firm, Money.Zero),
                [Household] = new CoreEntity(Household, CoreEntityKind.Household, Money.Zero)
            },
            Regions = new Dictionary<RegionId, Region>
            {
                [Region] = new(Region, 100, 100m, 100m)
            },
            Resources = new Dictionary<ResourceId, Resource>
            {
                [Iron] = new(Iron, "Iron", ResourceKind.IntermediateGood, IronAsset)
            }
        };

    [Test]
    public async Task HoldingLedger_WritesAuthoritativeEconomicPosition()
    {
        var state = HoldingLedger.Credit(StateWithCatalog(), Firm, Region, Iron, 100m);
        var owner = EconomicIdentity.For(Firm);
        var key = PositionLedger.Key(owner, Region, IronAsset);

        await Assert.That(state.PositionState[key].Quantity).IsEqualTo(100m);
        await Assert.That(HoldingLedger.GetQuantity(state, Firm, Region, Iron)).IsEqualTo(100m);
    }

    [Test]
    public async Task CashLedger_UsesMonetaryPositionAsAuthority()
    {
        var state = CashLedger.SetCash(StateWithCatalog(), Firm, Money.From(500m));

        await Assert.That(CashLedger.Balance(state, Firm)).IsEqualTo(Money.From(500m));
        await Assert.That(
            PositionLedger.GetQuantity(
                state,
                EconomicIdentity.For(Firm),
                region: null,
                state.MonetaryAssetId)).IsEqualTo(500m);
    }

    [Test]
    public async Task TransactionEngine_AppliesPurchaseAtomically()
    {
        var state = HoldingLedger.Credit(StateWithCatalog(), Firm, Region, Iron, 10m);
        var transaction = new EconomicTransaction(
            TransactionId.From(Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff")),
            [
                new PositionChange(EconomicIdentity.For(Firm), IronAsset, -4m, Region),
                new PositionChange(EconomicIdentity.For(Household), IronAsset, 4m, Region)
            ],
            "purchase");

        var next = EconomicTransactionEngine.Apply(state, transaction);

        await Assert.That(
            PositionLedger.GetQuantity(next, EconomicIdentity.For(Firm), Region, IronAsset))
            .IsEqualTo(6m);
        await Assert.That(
            PositionLedger.GetQuantity(next, EconomicIdentity.For(Household), Region, IronAsset))
            .IsEqualTo(4m);
    }

    [Test]
    public async Task TransactionEngine_DoesNotReturnPartialStateOnFailedDebit()
    {
        var state = HoldingLedger.Credit(StateWithCatalog(), Firm, Region, Iron, 2m);
        var transaction = new EconomicTransaction(
            TransactionId.From(Guid.Parse("11111111-2222-3333-4444-555555555555")),
            [
                new PositionChange(EconomicIdentity.For(Firm), IronAsset, -1m, Region),
                new PositionChange(EconomicIdentity.For(Household), IronAsset, -5m, Region)
            ]);

        var act = () => EconomicTransactionEngine.Apply(state, transaction);

        await Assert.That(act).Throws<InvalidOperationException>();
        await Assert.That(HoldingLedger.GetQuantity(state, Firm, Region, Iron)).IsEqualTo(2m);
    }

    [Test]
    public async Task ClaimBook_UsesAssetDenominatedPrincipal()
    {
        var state = StateWithCatalog();
        var loanId = LoanId.From(Guid.Parse("12121212-1212-1212-1212-121212121212"));
        var loan = new Loan(
            loanId,
            Firm,
            Household,
            Money.From(100m),
            0.01m,
            4,
            LoanStatus.Performing);
        var claim = ClaimLedger.FromLoan(state, loan);
        state = EconomicTransactionEngine.Apply(
            state,
            new EconomicTransaction(
                TransactionId.From(Guid.Parse("13131313-1313-1313-1313-131313131313")),
                [new CreateClaim(claim)]));

        var settled = EconomicTransactionEngine.Apply(
            state,
            new EconomicTransaction(
                TransactionId.From(Guid.Parse("14141414-1414-1414-1414-141414141414")),
                [new SettleClaim(claim.Id, new AssetAmount(state.MonetaryAssetId, 25m))]));

        await Assert.That(ClaimLedger.Get(settled, loanId).Principal.Quantity).IsEqualTo(75m);
    }

    [Test]
    public async Task Valuation_RequiresPostedPrice()
    {
        var state = HoldingLedger.Credit(StateWithCatalog(), Firm, Region, Iron, 10m);
        var position = PositionLedger.Snapshot(state).Values.Single();
        var context = new ValuationContext(state.MonetaryAssetId, state.Period);

        await Assert.That(Valuation.TryValue(state, position, context, out _)).IsFalse();

        state = state with
        {
            PostedPrices = new Dictionary<string, PostedPrice>
            {
                [EconomyState.PriceKey(Region, Iron)] =
                    new(Region, Iron, Money.From(7m))
            }
        };

        await Assert.That(Valuation.TryValue(state, position, context, out var value)).IsTrue();
        await Assert.That(value).IsEqualTo(Money.From(70m));
    }

    [Test]
    public async Task DeterministicIds_AreStableForTheSameState()
    {
        var first = DeterministicIds.ObligationIdFor(
            StateWithCatalog(),
            Firm,
            Household,
            Money.From(25m),
            1,
            ObligationKind.Trade);
        var second = DeterministicIds.ObligationIdFor(
            StateWithCatalog(),
            Firm,
            Household,
            Money.From(25m),
            1,
            ObligationKind.Trade);

        await Assert.That(first).IsEqualTo(second);
    }

    [Test]
    public async Task ModelSpecification_IsSerializable()
    {
        var json = JsonSerializer.Serialize(EconomyModelSpecification.Default);
        var restored = JsonSerializer.Deserialize<EconomyModelSpecification>(json);

        await Assert.That(restored).IsEqualTo(EconomyModelSpecification.Default);
    }

    [Test]
    public async Task NamedEntropyStreams_AreStableAndIndependent()
    {
        var first = new SimulationEntropy(42);
        var second = new SimulationEntropy(42);

        var firstMarkets = first.Stream("markets");
        var firstFirms = first.Stream("firms/a");
        var secondMarkets = second.Stream("markets");

        await Assert.That(firstMarkets.NextInt(10_000)).IsEqualTo(secondMarkets.NextInt(10_000));
        await Assert.That(firstFirms.NextInt(10_000)).IsNotEqualTo(firstMarkets.NextInt(10_000));
    }
}
