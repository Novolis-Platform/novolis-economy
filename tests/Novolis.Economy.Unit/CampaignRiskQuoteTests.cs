using Novolis.Economy.Logistics;
using TUnit.Core;

namespace Novolis.Economy.Unit;

/// <summary>Hull risk quote + transit wear monotone properties (library APIs).</summary>
public sealed class CampaignRiskQuoteTests
{
  [Test]
  public async Task Premium_Quote_Is_Monotone_In_Life_Fraction_And_Actuarial_Load()
  {
    static decimal Quote(decimal lifeFrac, int priorityLegs, int longLanes, decimal load) =>
      HullRiskQuotes.DailyPremium(
        basePremium: 32m,
        lifeFraction: lifeFrac,
        priorityLegs: priorityLegs,
        longLaneLegs: longLanes,
        actuarialLoad: load,
        idleOrSuspended: false);

    await Assert.That(Quote(0m, 0, 0, 1m)).IsLessThan(Quote(0.5m, 0, 0, 1m));
    await Assert.That(Quote(0.5m, 0, 0, 1m)).IsLessThan(Quote(0.5m, 20, 0, 1m));
    await Assert.That(Quote(0.5m, 20, 0, 1m)).IsLessThan(Quote(0.5m, 20, 20, 1m));
    await Assert.That(Quote(0.5m, 20, 20, 1m)).IsLessThan(Quote(0.5m, 20, 20, 1.35m));
  }

  [Test]
  public async Task Idle_Premium_Is_Standing_Fee_Only()
  {
    var operating = HullRiskQuotes.DailyPremium(32m, 0.5m, 10, 5, 1m, idleOrSuspended: false);
    var idle = HullRiskQuotes.DailyPremium(32m, 0.5m, 10, 5, 1m, idleOrSuspended: true);
    await Assert.That(idle).IsLessThan(operating);
    await Assert.That(idle).IsEqualTo(8m);
  }

  [Test]
  public async Task Wear_Hour_Is_Monotone_In_Mass_Load_And_Priority()
  {
    var emptyStd = TransitProfiles.WearForUnderwayHour(TransitProfile.StandardCommercial, 0m, 1m);
    var fullStd = TransitProfiles.WearForUnderwayHour(TransitProfile.StandardCommercial, 1m, 1m);
    var fullPri = TransitProfiles.WearForUnderwayHour(TransitProfile.PriorityCommercial, 1m, 1m);
    var fullPriHard = TransitProfiles.WearForUnderwayHour(TransitProfile.PriorityCommercial, 1m, 3m);

    await Assert.That(emptyStd).IsLessThan(fullStd);
    await Assert.That(fullStd).IsLessThan(fullPri);
    await Assert.That(fullPri).IsLessThan(fullPriHard);
  }

  [Test]
  public async Task Priority_Wear_Claim_Threshold_Aligns_With_Higher_Wear_Rate()
  {
    const decimal claimThreshold = 8m;
    var slow = TransitProfiles.Factors(TransitProfile.SlowEconomic).WearPerUnderwayHour;
    var priority = TransitProfiles.Factors(TransitProfile.PriorityCommercial).WearPerUnderwayHour;
    await Assert.That(priority).IsGreaterThan(slow);

    var hoursToClaimSlow = claimThreshold / slow;
    var hoursToClaimPri = claimThreshold / priority;
    await Assert.That(hoursToClaimPri).IsLessThan(hoursToClaimSlow);
  }

  [Test]
  public async Task Elective_Overhaul_Window_Is_Before_Rated_Life()
  {
    var electiveAt = FtlDriveLifePolicy.RatedLifeLight * FtlDriveLifePolicy.ElectiveOverhaulFraction;
    await Assert.That(electiveAt).IsLessThan(FtlDriveLifePolicy.RatedLifeLight);
  }

  [Test]
  public async Task Burnout_Overhaul_Exceeds_Elective()
  {
    var elective = HullRiskQuotes.ElectiveOverhaul(5_000m);
    var burnout = HullRiskQuotes.BurnoutOverhaul(5_000m);
    await Assert.That(burnout).IsGreaterThan(elective);
  }
}
