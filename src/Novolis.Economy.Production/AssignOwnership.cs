using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Set an absolute ownership fraction on an issuer.</summary>
public sealed record AssignOwnership(
  FirmId IssuerFirmId,
  FirmId OwnerFirmId,
  decimal Fraction) : IEconomyCommand;
