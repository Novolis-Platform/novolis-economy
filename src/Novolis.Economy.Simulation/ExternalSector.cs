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
