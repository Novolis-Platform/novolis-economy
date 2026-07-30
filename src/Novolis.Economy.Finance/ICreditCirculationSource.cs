using Novolis.Economy;
using Novolis.Economy.Production;

namespace Novolis.Economy.Finance;

/// <summary>
/// Simulation-facing surface for <see cref="CreditCirculation"/>.
/// Implemented in <c>Novolis.Economy.Simulation</c> to avoid a Finance↔Simulation project cycle.
/// </summary>
public interface ICreditCirculationSource
{
  /// <summary>Accumulated economy events.</summary>
  IReadOnlyList<IEconomyEvent> Events { get; }

  /// <summary>Current simulation clock.</summary>
  SimulationHour Clock { get; }

  /// <summary>Firm cash + household budgets.</summary>
  decimal LiquidStock { get; }

  /// <summary>Sum of cohort budget remaining.</summary>
  decimal HouseholdBudgets { get; }

  /// <summary>Sum of firm ledger cash.</summary>
  decimal FirmCash { get; }

  /// <summary>Inventory book value (qty × unit cost).</summary>
  decimal InventoryBookValue { get; }

  /// <summary>Cargo delivered aggregate.</summary>
  decimal CargoDelivered { get; }

  /// <summary>Active loan count.</summary>
  int ActiveLoanCount { get; }

  /// <summary>Principal on active + defaulted loans.</summary>
  decimal PrincipalOutstanding { get; }

  /// <summary>Firms with credit frozen.</summary>
  int CreditFrozenFirmCount { get; }

  /// <summary>Physical inventory quantity for a product across all locations.</summary>
  decimal InventoryQuantity(ProductId productId);

  /// <summary>Core period index.</summary>
  int CorePeriod { get; }

  /// <summary>Core total cash.</summary>
  decimal CoreTotalCash { get; }

  /// <summary>Sum of Core holding quantities.</summary>
  decimal CoreHoldingQty { get; }

  /// <summary>Core holding slot count.</summary>
  int CoreHoldingSlots { get; }

  /// <summary>Core in-flight transfer count.</summary>
  int CoreInFlightTransfers { get; }
}
