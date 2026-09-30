namespace Novolis.Economy.Simulation;

/// <summary>Observable settlement totals for an explicit external sector.</summary>
public sealed class ExternalTradeLedger
{
  /// <summary>Import purchases paid to the external sector.</summary>
  public Money ImportsPaid { get; set; }

  /// <summary>Export receipts paid by the external sector.</summary>
  public Money ExportsReceived { get; set; }

  /// <summary>Physical quantity imported by product.</summary>
  public Dictionary<ProductId, Quantity> ImportsByProduct { get; } = new();

  /// <summary>Physical quantity exported by product.</summary>
  public Dictionary<ProductId, Quantity> ExportsByProduct { get; } = new();

  /// <summary>Net external monetary balance from the domestic perspective.</summary>
  public Money TradeBalance => ExportsReceived - ImportsPaid;
}
