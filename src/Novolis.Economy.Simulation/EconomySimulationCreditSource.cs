using Novolis.Economy;
using Novolis.Economy.Core;
using Novolis.Economy.Core.Extensions;
using Novolis.Economy.Finance;
using Novolis.Economy.Production;

namespace Novolis.Economy.Simulation;

/// <summary>Adapts <see cref="EconomySimulation"/> for <see cref="CreditCirculation"/>.</summary>
public sealed class EconomySimulationCreditSource : ICreditCirculationSource
{
  private readonly EconomySimulation _sim;

  /// <summary>Creates a source over a live simulation.</summary>
  public EconomySimulationCreditSource(EconomySimulation sim)
  {
    ArgumentNullException.ThrowIfNull(sim);
    _sim = sim;
  }

  /// <inheritdoc />
  public IReadOnlyList<IEconomyEvent> Events => _sim.State.Events;

  /// <inheritdoc />
  public SimulationHour Clock => _sim.State.Clock;

  /// <inheritdoc />
  public decimal LiquidStock => MoneyStock.Liquid(_sim.State.World);

  /// <inheritdoc />
  public decimal HouseholdBudgets =>
    _sim.State.World.Cohorts.Sum(c => c.BudgetRemaining.Amount);

  /// <inheritdoc />
  public decimal FirmCash =>
    _sim.State.World.Ledgers.Values.Sum(l => l.Cash.Amount);

  /// <inheritdoc />
  public decimal InventoryBookValue
  {
    get
    {
      var inv = _sim.State.World.Inventory;
      var sum = 0m;
      foreach (var key in inv.Keys)
      {
        foreach (var lot in inv.GetLots(key))
        {
          sum += lot.Quantity.Value * lot.UnitCost.Amount;
        }
      }

      return sum;
    }
  }

  /// <inheritdoc />
  public decimal CargoDelivered => _sim.State.World.TransportStats.CargoDelivered.Value;

  /// <inheritdoc />
  public int ActiveLoanCount =>
    _sim.State.World.Loans.Count(l => l.Status == Novolis.Economy.Finance.LoanStatus.Active);

  /// <inheritdoc />
  public decimal PrincipalOutstanding =>
    _sim.State.World.Loans
      .Where(l => l.Status is Novolis.Economy.Finance.LoanStatus.Active
        or Novolis.Economy.Finance.LoanStatus.Defaulted)
      .Sum(l => l.PrincipalRemaining.Amount);

  /// <inheritdoc />
  public int CreditFrozenFirmCount =>
    _sim.State.World.Entities.Values.Count(e => e.CreditFrozen);

  /// <inheritdoc />
  public decimal InventoryQuantity(ProductId productId)
  {
    var world = _sim.State.World;
    var sum = 0m;
    foreach (var key in world.Inventory.Keys)
    {
      if (!key.ProductId.Equals(productId))
      {
        continue;
      }

      sum += world.Inventory.GetLots(key).Sum(l => l.Quantity.Value);
    }

    return sum;
  }

  /// <inheritdoc />
  public int CorePeriod => _sim.State.World.CoreState.Snapshot().Period;

  /// <inheritdoc />
  public decimal CoreTotalCash => _sim.State.World.CoreState.Snapshot().TotalCash.Amount;

  /// <inheritdoc />
  public decimal CoreHoldingQty =>
    _sim.State.World.CoreState.Holdings.Values.Sum(h => h.Quantity);

  /// <inheritdoc />
  public int CoreHoldingSlots => _sim.State.World.CoreState.Snapshot().HoldingSlots;

  /// <inheritdoc />
  public int CoreInFlightTransfers => _sim.State.World.CoreState.Snapshot().InFlightTransfers;
}
