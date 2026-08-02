using Novolis.Economy;
using Novolis.Economy.Accounting;

namespace Novolis.Economy.Unit;

public sealed class OwnershipEngineTests
{
    private static readonly FirmId Issuer = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000b1"));
    private static readonly FirmId OwnerA = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000b2"));
    private static readonly FirmId OwnerB = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000b3"));
    private static readonly FirmId OwnerC = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000b4"));
    private static readonly SimulationDate Date = SimulationDate.Epoch;

    private static bool CanIssue(FirmId _) => true;

    [Test]
    public async Task TryAssign_AddsAndUpdatesClaims()
    {
        var claims = new List<OwnershipClaim>();
        await Assert.That(OwnershipEngine.TryAssign(claims, Issuer, OwnerA, 0.6m, CanIssue)).IsTrue();
        await Assert.That(claims.Single().Fraction).IsEqualTo(0.6m);

        await Assert.That(OwnershipEngine.TryAssign(claims, Issuer, OwnerB, 0.4m, CanIssue)).IsTrue();
        await Assert.That(claims.Sum(c => c.Fraction)).IsEqualTo(1m);

        await Assert.That(OwnershipEngine.TryAssign(claims, Issuer, OwnerA, 0.5m, CanIssue)).IsTrue();
        await Assert.That(claims.First(c => c.OwnerFirmId.Equals(OwnerA)).Fraction).IsEqualTo(0.5m);
    }

    [Test]
    public async Task TryAssign_RejectsOverAllocation()
    {
        var claims = new List<OwnershipClaim> { new(Issuer, OwnerA, 0.7m) };
        await Assert.That(OwnershipEngine.TryAssign(claims, Issuer, OwnerB, 0.4m, CanIssue)).IsFalse();
    }

    [Test]
    public async Task TryAssign_RemovesClaim_WhenFractionZero()
    {
        var claims = new List<OwnershipClaim> { new(Issuer, OwnerA, 0.5m) };
        await Assert.That(OwnershipEngine.TryAssign(claims, Issuer, OwnerA, 0m, CanIssue)).IsTrue();
        await Assert.That(claims.Count).IsEqualTo(0);
    }

    [Test]
    public async Task TryTransfer_MovesFractionBetweenOwners()
    {
        var claims = new List<OwnershipClaim> { new(Issuer, OwnerA, 1m) };
        await Assert.That(OwnershipEngine.TryTransfer(claims, Issuer, OwnerA, OwnerB, 0.25m, CanIssue)).IsTrue();
        await Assert.That(claims.First(c => c.OwnerFirmId.Equals(OwnerA)).Fraction).IsEqualTo(0.75m);
        await Assert.That(claims.First(c => c.OwnerFirmId.Equals(OwnerB)).Fraction).IsEqualTo(0.25m);
    }

    [Test]
    public async Task TransferAllIssuerClaimsTo_MergesIntoNewOwner()
    {
        var claims = new List<OwnershipClaim>
        {
            new(Issuer, OwnerA, 0.6m),
            new(Issuer, OwnerB, 0.4m),
        };
        OwnershipEngine.TransferAllIssuerClaimsTo(claims, Issuer, OwnerC);
        await Assert.That(claims.Count).IsEqualTo(1);
        await Assert.That(claims[0].OwnerFirmId).IsEqualTo(OwnerC);
        await Assert.That(claims[0].Fraction).IsEqualTo(1m);
    }

    [Test]
    public async Task TryDeclareDividend_PaysProRata_AndConservesCash()
    {
        var claims = new List<OwnershipClaim>
        {
            new(Issuer, OwnerA, 0.75m),
            new(Issuer, OwnerB, 0.25m),
        };
        var issuerLedger = new FirmLedger(Issuer);
        issuerLedger.SeedCash(Money.From(1_000m), Date);
        var ledgers = new Dictionary<FirmId, FirmLedger>
        {
            [Issuer] = issuerLedger,
            [OwnerA] = new FirmLedger(OwnerA),
            [OwnerB] = new FirmLedger(OwnerB),
        };
        var open = ledgers.Values.Sum(l => l.Cash.Amount);

        var paid = OwnershipEngine.TryDeclareDividend(claims, ledgers, Issuer, Money.From(400m), Date);

        await Assert.That(paid.Count).IsEqualTo(2);
        await Assert.That(paid.Sum(p => p.Amount.Amount)).IsGreaterThan(0m);
        await Assert.That(ledgers[Issuer].Cash.Amount).IsLessThan(1_000m);
        await Assert.That(ledgers.Values.Sum(l => l.Cash.Amount)).IsEqualTo(open);
    }

    [Test]
    public async Task TryDeclareDividend_CreditsHouseholdBudget()
    {
        var claims = new List<OwnershipClaim> { new(Issuer, OwnerA, 1m) };
        var issuerLedger = new FirmLedger(Issuer);
        issuerLedger.SeedCash(Money.From(200m), Date);
        var ledgers = new Dictionary<FirmId, FirmLedger>
        {
            [Issuer] = issuerLedger,
            [OwnerA] = new FirmLedger(OwnerA),
        };
        Money? budgetCredit = null;
        var paid = OwnershipEngine.TryDeclareDividend(
            claims, ledgers, Issuer, Money.From(50m), Date,
            isHousehold: id => id.Equals(OwnerA),
            creditHouseholdBudget: (_, m) => budgetCredit = m);

        await Assert.That(paid.Count).IsEqualTo(1);
        await Assert.That(budgetCredit!.Value.Amount).IsEqualTo(50m);
        await Assert.That(ledgers[OwnerA].Cash.Amount).IsEqualTo(0m);
    }

    [Test]
    public async Task TryAddClaim_AccumulatesBuyerStake()
    {
        var claims = new List<OwnershipClaim>();
        await Assert.That(OwnershipEngine.TryAddClaim(claims, Issuer, OwnerA, 0.3m, CanIssue)).IsTrue();
        await Assert.That(OwnershipEngine.TryAddClaim(claims, Issuer, OwnerA, 0.2m, CanIssue)).IsTrue();
        await Assert.That(claims.Single().Fraction).IsEqualTo(0.5m);
    }

    [Test]
    public async Task PostOwnershipSaleProceeds_AndCapacityInvestment()
    {
        var ledger = new FirmLedger(Issuer);
        ledger.SeedCash(Money.From(100m), Date);
        OwnershipEngine.PostOwnershipSaleProceeds(ledger, Money.From(25m), Date);
        await Assert.That(ledger.Cash.Amount).IsEqualTo(125m);

        await Assert.That(OwnershipEngine.TryPostCapacityInvestment(ledger, Money.From(30m), Date)).IsTrue();
        await Assert.That(ledger.Cash.Amount).IsEqualTo(95m);

        await Assert.That(OwnershipEngine.TryPostCapacityInvestment(ledger, Money.From(200m), Date)).IsFalse();
    }
}
