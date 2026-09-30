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
