using System.Collections.Immutable;
using Novolis.Economy;
using Novolis.Economy.Agents;
using Novolis.Economy.Accounting;
using Novolis.Economy.Core;
using Novolis.Economy.Core.Finance;
using Novolis.Economy.Logistics;
using Novolis.Economy.Markets;
using Novolis.Economy.Population;
using Novolis.Economy.Production;

namespace Novolis.Economy.Simulation;

/// <summary>Running transport economics counters for scenarios.</summary>
public sealed class TransportAggregates
{
  /// <summary>Cargo quantity delivered via multi-leg shipments.</summary>
  public Quantity CargoDelivered { get; set; }

  /// <summary>Fuel units burned.</summary>
  public Quantity FuelBurned { get; set; }

  /// <summary>Ledger value of burned fuel.</summary>
  public Money FuelBurnValue { get; set; }

  /// <summary>Fuel units bunkered from hubs.</summary>
  public Quantity FuelBunkered { get; set; }

  /// <summary>Tolls paid.</summary>
  public Money TollsPaid { get; set; }

  /// <summary>Crew labor hours while underway.</summary>
  public decimal CrewLaborHours { get; set; }

  /// <summary>Failed plan attempts.</summary>
  public int FailedPlans { get; set; }

  /// <summary>Sum of hours from depart to deliver for completed multi-leg shipments.</summary>
  public long TransitHoursSum { get; set; }

  /// <summary>Count of completed multi-leg deliveries (for average transit).</summary>
  public int TransitSampleCount { get; set; }

  /// <summary>Accumulated drive wear units from underway hours × profile wear factor.</summary>
  public decimal DriveWearAccumulated { get; set; }
}
