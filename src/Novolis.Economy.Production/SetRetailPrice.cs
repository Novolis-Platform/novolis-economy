using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Command to set a retail shelf price.</summary>
public sealed record SetRetailPrice(
  FirmId FirmId,
  FacilityId FacilityId,
  ProductId ProductId,
  Money Price) : IEconomyCommand;
