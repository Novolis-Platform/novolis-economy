using Novolis.Economy;
using Novolis.Economy.Production;

namespace Novolis.Economy.Finance;

/// <summary>Point-in-time macro snapshot for milestone comparison.</summary>
public readonly record struct MacroSnapshot(
  long HourIndex,
  int DayIndex,
  decimal Liquid,
  decimal Households,
  decimal FirmCash,
  decimal InventoryBook,
  decimal Produced,
  decimal RetailSold,
  int BookFills,
  decimal Delivered,
  int Departed,
  int LoansDefaulted,
  decimal DividendsPaid,
  int FacilitiesAbsorbed,
  int Upgrades,
  decimal SkuRaw,
  decimal SkuCapital,
  decimal SkuFinal,
  decimal SkuEnergy,
  int CorePeriod,
  decimal CoreCash,
  decimal CoreHoldingQty,
  int CoreHoldingSlots,
  int CoreInFlight);
