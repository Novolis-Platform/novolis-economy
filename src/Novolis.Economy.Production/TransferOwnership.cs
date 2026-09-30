using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Move ownership fraction between owners of the same issuer.</summary>
public sealed record TransferOwnership(
  FirmId IssuerFirmId,
  FirmId FromOwnerFirmId,
  FirmId ToOwnerFirmId,
  decimal Fraction) : IEconomyCommand;
