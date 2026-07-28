using Novolis.Economy;
using Novolis.Economy.Logistics;
using TUnit.Core;

namespace Novolis.Economy.Unit;

public sealed class TransitProfileAndPremiumTests
{
  [Test]
  public async Task Priority_CostsMoreFuelAndLessHours_ThanSlow()
  {
    var corridor = new TransportCorridor(
      TransportCorridorId.From(Guid.Parse("00000000-0000-4000-8000-000000000001")),
      TransportHubId.From(Guid.Parse("00000000-0000-4000-8000-000000000010")),
      TransportHubId.From(Guid.Parse("00000000-0000-4000-8000-000000000011")),
      TransitHours: 100,
      MaxCargo: Quantity.From(50m),
      Difficulty: 1m,
      Toll: Money.From(1m));
    var vehicle = new VehicleClass(
      VehicleClassId.From(Guid.Parse("00000000-0000-4000-8000-000000000020")),
      Quantity.From(40m),
      FuelBurnPerDifficultyHour: 0.1m,
      CrewLaborPerUnderwayHour: 0.02m,
      FuelTankCapacity: Quantity.From(80m));

    var slowH = TransitProfiles.EffectiveHours(corridor, TransitProfile.SlowEconomic);
    var priH = TransitProfiles.EffectiveHours(corridor, TransitProfile.PriorityCommercial);
    var slowFuel = ItineraryPlanner.FuelBurnForLeg(corridor, vehicle, TransitProfile.SlowEconomic).Value;
    var priFuel = ItineraryPlanner.FuelBurnForLeg(corridor, vehicle, TransitProfile.PriorityCommercial).Value;

    await Assert.That(priH).IsLessThan(slowH);
    await Assert.That(priFuel).IsGreaterThan(slowFuel);
    await Assert.That(TransitProfiles.Factors(TransitProfile.PriorityCommercial).WearPerUnderwayHour)
      .IsGreaterThan(TransitProfiles.Factors(TransitProfile.SlowEconomic).WearPerUnderwayHour);
  }

  [Test]
  public async Task Wear_Raises_Effective_Fuel_And_Hours_Monotone_Across_Profiles()
  {
    var factorsSlow = TransitProfiles.Factors(TransitProfile.SlowEconomic);
    var factorsStd = TransitProfiles.Factors(TransitProfile.StandardCommercial);
    var factorsPri = TransitProfiles.Factors(TransitProfile.PriorityCommercial);
    await Assert.That(factorsSlow.FuelFactor).IsLessThan(factorsStd.FuelFactor);
    await Assert.That(factorsStd.FuelFactor).IsLessThan(factorsPri.FuelFactor);
    await Assert.That(factorsPri.HoursFactor).IsLessThan(factorsStd.HoursFactor);
    await Assert.That(factorsStd.HoursFactor).IsLessThan(factorsSlow.HoursFactor);
  }
}
