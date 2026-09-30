using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Pay a cash dividend from issuer to claim holders (pro-rata).</summary>
public sealed record DeclareDividend(
  FirmId IssuerFirmId,
  Money Total) : IEconomyCommand;
