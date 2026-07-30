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

/// <summary>Dashboard metrics for liquid stock, imports, finance, macro events, and activity.</summary>
public sealed class CreditCirculation
{
  private readonly ICreditCirculationSource _source;
  private int _eventCursor;
  private decimal _wagesDistributed;
  private decimal _importSpend;
  private decimal _exportRevenue;
  private decimal _exportQty;
  private int _exportFills;
  private int _trampVentures;
  private decimal _tollsToTreasury;
  private decimal _produced;
  private decimal _retailSold;
  private int _bookFills;
  private decimal _bookFillQty;
  private int _departed;
  private decimal _interestAccrued;
  private decimal _interestPaid;
  private int _loansOriginated;
  private int _loansDefaulted;
  private int _dividends;
  private decimal _dividendCash;
  private int _ownershipChanges;
  private int _creditFreezes;
  private int _facilitiesAbsorbed;
  private int _facilityUpgrades;
  private int _facilityUpgradeFails;
  private int _b2bFills;
  private decimal _b2bQty;
  private int _b2bFailCash;
  private int _b2bFailStock;
  private readonly Dictionary<string, int> _planFailReasons = new(StringComparer.Ordinal);
  private readonly List<string> _macroLog = [];
  private readonly List<MacroSnapshot> _milestones = [];
  private int _lastMilestoneDay = -1;
  private Dictionary<FirmId, string> _firmNames = new();
  private ProductId? _ore;
  private ProductId? _parts;
  private ProductId? _goods;
  private ProductId? _fuel;

  /// <summary>Creates a circulation tracker over a live simulation source.</summary>
  public CreditCirculation(ICreditCirculationSource source)
  {
    ArgumentNullException.ThrowIfNull(source);
    _source = source;
    _eventCursor = source.Events.Count;
  }

  /// <summary>Optional display names for macro event log.</summary>
  public void SetFirmNames(IEnumerable<(string Name, FirmId Id)> firms)
  {
    _firmNames = firms.ToDictionary(f => f.Id, f => f.Name);
  }

  /// <summary>SKU ids for inventory-by-product milestones.</summary>
  public void SetSkuIds(ProductId ore, ProductId parts, ProductId goods, ProductId fuel)
  {
    _ore = ore;
    _parts = parts;
    _goods = goods;
    _fuel = fuel;
  }

  /// <summary>Cumulative household credits from wages.</summary>
  public decimal WagesDistributed => _wagesDistributed;

  /// <summary>Cumulative import spend.</summary>
  public decimal ImportSpend => _importSpend;

  /// <summary>Cumulative export revenue.</summary>
  public decimal ExportRevenue => _exportRevenue;

  /// <summary>Cumulative export quantity.</summary>
  public decimal ExportQty => _exportQty;

  /// <summary>Export fill count.</summary>
  public int ExportFills => _exportFills;

  /// <summary>Hull loans tagged as tramp ventures.</summary>
  public int TrampVentures => _trampVentures;

  /// <summary>Cumulative corridor tolls paid to treasury.</summary>
  public decimal TollsToTreasury => _tollsToTreasury;

  /// <summary>Cumulative production quantity.</summary>
  public decimal Produced => _produced;

  /// <summary>Cumulative retail sold quantity.</summary>
  public decimal RetailSold => _retailSold;

  /// <summary>Hub book fill count.</summary>
  public int BookFills => _bookFills;

  /// <summary>Hub book fill quantity.</summary>
  public decimal BookFillQty => _bookFillQty;

  /// <summary>Shipment departures.</summary>
  public int Departed => _departed;

  /// <summary>Interest accrued on loans.</summary>
  public decimal InterestAccrued => _interestAccrued;

  /// <summary>Interest paid via repayments.</summary>
  public decimal InterestPaid => _interestPaid;

  /// <summary>Loans originated.</summary>
  public int LoansOriginated => _loansOriginated;

  /// <summary>Loans defaulted.</summary>
  public int LoansDefaulted => _loansDefaulted;

  /// <summary>Dividend payment count.</summary>
  public int Dividends => _dividends;

  /// <summary>Cumulative dividend cash.</summary>
  public decimal DividendCash => _dividendCash;

  /// <summary>Ownership change events.</summary>
  public int OwnershipChanges => _ownershipChanges;

  /// <summary>Credit freeze events.</summary>
  public int CreditFreezes => _creditFreezes;

  /// <summary>Facilities absorbed.</summary>
  public int FacilitiesAbsorbed => _facilitiesAbsorbed;

  /// <summary>Successful facility upgrades.</summary>
  public int FacilityUpgrades => _facilityUpgrades;

  /// <summary>Failed facility upgrades.</summary>
  public int FacilityUpgradeFails => _facilityUpgradeFails;

  /// <summary>Inter-firm goods sale fills.</summary>
  public int B2bFills => _b2bFills;

  /// <summary>Inter-firm goods sale quantity.</summary>
  public decimal B2bQty => _b2bQty;

  /// <summary>B2B failures due to cash.</summary>
  public int B2bFailCash => _b2bFailCash;

  /// <summary>B2B failures due to stock.</summary>
  public int B2bFailStock => _b2bFailStock;

  /// <summary>Shipment plan failure reasons.</summary>
  public IReadOnlyDictionary<string, int> PlanFailReasons => _planFailReasons;

  /// <summary>Recent macro event log lines.</summary>
  public IReadOnlyList<string> MacroLog => _macroLog;

  /// <summary>Captured day milestones.</summary>
  public IReadOnlyList<MacroSnapshot> Milestones => _milestones;

  /// <summary>Current liquid money stock.</summary>
  public decimal LiquidStock => _source.LiquidStock;

  /// <summary>Active loan count.</summary>
  public int ActiveLoans => _source.ActiveLoanCount;

  /// <summary>Principal on active and defaulted loans.</summary>
  public decimal PrincipalOutstanding => _source.PrincipalOutstanding;

  /// <summary>Firms with credit frozen.</summary>
  public int CreditFrozenFirms => _source.CreditFrozenFirmCount;

  /// <summary>Inventory book value.</summary>
  public decimal InventoryBookValue => _source.InventoryBookValue;

  /// <summary>Observe events appended since the last pulse.</summary>
  public void ObserveAfterPulse(int eventsBeforePulse)
  {
    var events = _source.Events;
    var clock = _source.Clock;
    for (var i = Math.Max(eventsBeforePulse, _eventCursor); i < events.Count; i++)
    {
      switch (events[i])
      {
        case HouseholdCreditsIssued e:
          _wagesDistributed += e.Amount.Amount;
          break;
        case ProcurementFilled e:
          _importSpend += e.UnitPrice.Amount * e.Quantity.Value;
          break;
        case ExportFilled e:
          _exportFills++;
          _exportQty += e.Quantity.Value;
          _exportRevenue += e.Revenue.Amount;
          break;
        case TransportTollPaid e:
          _tollsToTreasury += e.Amount.Amount;
          break;
        case BatchProduced e:
          _produced += e.Quantity.Value;
          break;
        case GoodsSold e:
          _retailSold += e.Quantity.Value;
          break;
        case GoodsSoldInterFirm e:
          _b2bFills++;
          _b2bQty += e.Quantity.Value;
          break;
        case TransferGoodsFailed e:
          if (string.Equals(e.Reason, "cash", StringComparison.OrdinalIgnoreCase))
          {
            _b2bFailCash++;
          }
          else
          {
            _b2bFailStock++;
          }

          break;
        case HubOrderFilled e:
          _bookFills++;
          _bookFillQty += e.Quantity.Value;
          break;
        case ShipmentDeparted:
          _departed++;
          break;
        case ShipmentPlanFailed e:
          _planFailReasons[e.Reason] = _planFailReasons.GetValueOrDefault(e.Reason) + 1;
          break;
        case InterestAccrued e:
          _interestAccrued += e.Amount.Amount;
          break;
        case LoanRepaid e:
          _interestPaid += e.Amount.Amount;
          break;
        case LoanOriginated e:
          _loansOriginated++;
          Note(clock, $"loan {e.Principal.Amount:0} {Short(e.LenderFirmId)}→{Short(e.BorrowerFirmId)}");
          // Hull loans to venture tramp ids (…00c0+)
          if (e.BorrowerFirmId.Value.ToString("N").Contains("00000000c0", StringComparison.Ordinal))
          {
            _trampVentures++;
            Note(clock, $"VENTURE hull loan {e.Principal.Amount:0}");
          }

          break;
        case LoanDefaulted e:
          _loansDefaulted++;
          Note(clock, $"DEFAULT {Short(e.BorrowerFirmId)} owe {e.PrincipalRemaining.Amount:0}");
          break;
        case DividendPaid e:
          _dividends++;
          _dividendCash += e.Amount.Amount;
          Note(clock, $"dividend {e.Amount.Amount:0} {Short(e.IssuerFirmId)}→{Short(e.OwnerFirmId)}");
          break;
        case OwnershipChanged e:
          _ownershipChanges++;
          Note(clock, $"own {e.Fraction:0.##} of {Short(e.IssuerFirmId)} → {Short(e.OwnerFirmId)}");
          break;
        case CreditFrozenSet e:
          _creditFreezes++;
          Note(clock, $"credit FROZEN {Short(e.FirmId)}");
          break;
        case FacilityAbsorbed e:
          _facilitiesAbsorbed++;
          Note(clock, $"absorb facility {Short(e.FromFirmId)}→{Short(e.ToFirmId)}");
          break;
        case FacilityUpgraded e:
          _facilityUpgrades++;
          Note(clock, $"upgrade ×{e.CapacityFactor:0.##} cost {e.Cost.Amount:0} {Short(e.OwnerFirmId)}");
          break;
        case FacilityUpgradeFailed e:
          _facilityUpgradeFails++;
          Note(clock, $"upgrade FAIL {e.Reason}");
          break;
      }
    }

    _eventCursor = events.Count;
    MaybeCaptureMilestone();
  }

  /// <summary>Force a milestone at end-of-run if the last day wasn't already captured.</summary>
  public void CaptureFinalMilestone() => CaptureMilestone(force: true);

  private void MaybeCaptureMilestone()
  {
    var day = _source.Clock.Date.DayIndex;
    // Capture at 1, then every 100 days (100, 200, …).
    if (day == 1 || (day > 0 && day % 100 == 0 && day != _lastMilestoneDay))
    {
      CaptureMilestone(force: false);
    }
  }

  private void CaptureMilestone(bool force)
  {
    var day = _source.Clock.Date.DayIndex;
    if (!force && day == _lastMilestoneDay)
    {
      return;
    }

    if (force && _milestones.Count > 0 && _milestones[^1].DayIndex == day)
    {
      return;
    }

    _lastMilestoneDay = day;
    var sku = InventoryBySku();
    _milestones.Add(new MacroSnapshot(
      _source.Clock.HourIndex,
      day,
      LiquidStock,
      _source.HouseholdBudgets,
      _source.FirmCash,
      InventoryBookValue,
      _produced,
      _retailSold,
      _bookFills,
      _source.CargoDelivered,
      _departed,
      _loansDefaulted,
      _dividendCash,
      _facilitiesAbsorbed,
      _facilityUpgrades,
      sku.Raw,
      sku.Capital,
      sku.Final,
      sku.Energy,
      _source.CorePeriod,
      _source.CoreTotalCash,
      _source.CoreHoldingQty,
      _source.CoreHoldingSlots,
      _source.CoreInFlightTransfers));
  }

  /// <summary>Physical lots by campaign SKU (ops inventory; Core holdings are a parallel credit path).</summary>
  public (decimal Raw, decimal Capital, decimal Final, decimal Energy) InventoryBySku()
  {
    decimal raw = 0, capital = 0, final = 0, energy = 0;
    if (_ore is { } o)
    {
      raw = _source.InventoryQuantity(o);
    }

    if (_parts is { } p)
    {
      capital = _source.InventoryQuantity(p);
    }

    if (_goods is { } g)
    {
      final = _source.InventoryQuantity(g);
    }

    if (_fuel is { } f)
    {
      energy = _source.InventoryQuantity(f);
    }

    return (raw, capital, final, energy);
  }

  private void Note(SimulationHour clock, string text)
  {
    const int max = 40;
    var line = $"d{clock.Date.DayIndex}h{clock.HourIndex % 24} {text}";
    _macroLog.Add(line);
    if (_macroLog.Count > max)
    {
      _macroLog.RemoveAt(0);
    }
  }

  private string Short(FirmId id) =>
    _firmNames.TryGetValue(id, out var name) ? name : id.Value.ToString("N")[..8];
}
