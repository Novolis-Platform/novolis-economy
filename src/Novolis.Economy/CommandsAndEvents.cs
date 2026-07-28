namespace Novolis.Economy;

/// <summary>Command to set a retail shelf price.</summary>
/// <param name="FirmId">Owning firm.</param>
/// <param name="FacilityId">Retail facility.</param>
/// <param name="ProductId">Product offered.</param>
/// <param name="Price">New shelf price.</param>
public sealed record SetRetailPrice(
  FirmId FirmId,
  FacilityId FacilityId,
  ProductId ProductId,
  Money Price) : IEconomyCommand;

/// <summary>Event raised when a retail price changes.</summary>
/// <param name="Date">Simulation date of the change.</param>
/// <param name="FirmId">Owning firm.</param>
/// <param name="FacilityId">Retail facility.</param>
/// <param name="ProductId">Product offered.</param>
/// <param name="PreviousPrice">Price before the change.</param>
/// <param name="CurrentPrice">Price after the change.</param>
public sealed record RetailPriceChanged(
  SimulationDate Date,
  FirmId FirmId,
  FacilityId FacilityId,
  ProductId ProductId,
  Money PreviousPrice,
  Money CurrentPrice) : IEconomyEvent;
