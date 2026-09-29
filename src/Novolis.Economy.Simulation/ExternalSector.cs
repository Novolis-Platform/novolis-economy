namespace Novolis.Economy.Simulation;

/// <summary>Whether unbalanced domestic trade is allowed and how it is recorded.</summary>
public enum MonetaryClosure
{
  /// <summary>Require an explicit external counterparty for open flows.</summary>
  ExternalSector = 0,

  /// <summary>Require every monetary flow to have a domestic counterparty.</summary>
  Closed = 1,

  /// <summary>Permit recorded source/sink effects for game-friendly scenarios.</summary>
  Open = 2
}

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
