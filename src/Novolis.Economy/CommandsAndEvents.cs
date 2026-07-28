using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Command to set a retail shelf price.</summary>
public sealed record SetRetailPrice(
  FirmId FirmId,
  FacilityId FacilityId,
  ProductId ProductId,
  Money Price) : IEconomyCommand;

/// <summary>Event raised when a retail price changes.</summary>
public sealed record RetailPriceChanged(
  SimulationDate Date,
  FirmId FirmId,
  FacilityId FacilityId,
  ProductId ProductId,
  Money PreviousPrice,
  Money CurrentPrice) : IEconomyEvent;

/// <summary>Sets hourly production rate for a product at a facility.</summary>
public sealed record SetProductionPlan(
  FirmId FirmId,
  FacilityId FacilityId,
  ProductId ProductId,
  Quantity RatePerHour) : IEconomyCommand;

/// <summary>Production plan accepted.</summary>
public sealed record ProductionPlanSet(
  SimulationHour Hour,
  FirmId FirmId,
  FacilityId FacilityId,
  ProductId ProductId,
  Quantity RatePerHour) : IEconomyEvent;

/// <summary>Buy from the exogenous input market (infinite supply at the stated unit price ceiling).</summary>
public sealed record PlaceProcurementOrder(
  FirmId BuyerFirmId,
  InventoryLocationId Destination,
  ProductId ProductId,
  Quantity Quantity,
  Money MaxUnitPrice) : IEconomyCommand;

/// <summary>Issue a shipment along a freight route.</summary>
public sealed record IssueShipment(
  FirmId FirmId,
  FreightRouteId RouteId,
  ProductId ProductId,
  Quantity Quantity) : IEconomyCommand;

/// <summary>Plan and depart a multi-leg shipment between transport hubs.</summary>
public sealed record PlanShipment(
  FirmId FirmId,
  Guid OriginHubId,
  Guid DestinationHubId,
  ProductId ProductId,
  Quantity Quantity,
  Guid VehicleClassId) : IEconomyCommand;

/// <summary>Set available labor hours per firm per tick.</summary>
public sealed record SetAvailableLabor(
  FirmId FirmId,
  decimal HoursPerTick) : IEconomyCommand;

/// <summary>Inventory moved between locations or onto/from a shipment.</summary>
public sealed record InventoryTransferred(
  SimulationHour Hour,
  FirmId FirmId,
  ProductId ProductId,
  Quantity Quantity,
  string Reason) : IEconomyEvent;

/// <summary>A production batch was created.</summary>
public sealed record BatchProduced(
  SimulationHour Hour,
  FirmId FirmId,
  FacilityId FacilityId,
  ProductId ProductId,
  Quantity Quantity,
  Money UnitCost) : IEconomyEvent;

/// <summary>Goods sold to a consumer cohort.</summary>
public sealed record GoodsSold(
  SimulationHour Hour,
  FirmId FirmId,
  FacilityId FacilityId,
  ConsumerCohortId CohortId,
  ProductId ProductId,
  Quantity Quantity,
  Money UnitPrice,
  Money Revenue) : IEconomyEvent;

/// <summary>Invoice created (AR for seller / AP for buyer).</summary>
public sealed record InvoicePosted(
  SimulationHour Hour,
  Guid InvoiceId,
  FirmId SellerFirmId,
  FirmId? BuyerFirmId,
  Money Amount) : IEconomyEvent;

/// <summary>Invoice cash settlement.</summary>
public sealed record InvoiceSettled(
  SimulationHour Hour,
  Guid InvoiceId,
  Money AmountPaid) : IEconomyEvent;

/// <summary>Wages paid from cash.</summary>
public sealed record WagesPaid(
  SimulationHour Hour,
  FirmId FirmId,
  Money Amount) : IEconomyEvent;

/// <summary>Shipment left origin.</summary>
public sealed record ShipmentDeparted(
  SimulationHour Hour,
  Guid ShipmentId,
  FirmId FirmId,
  ProductId ProductId,
  Quantity Quantity) : IEconomyEvent;

/// <summary>Shipment arrived at destination.</summary>
public sealed record ShipmentDelivered(
  SimulationHour Hour,
  Guid ShipmentId,
  FirmId FirmId,
  ProductId ProductId,
  Quantity Quantity) : IEconomyEvent;

/// <summary>Shipment entered a corridor leg.</summary>
public sealed record ShipmentLegStarted(
  SimulationHour Hour,
  Guid ShipmentId,
  FirmId FirmId,
  Guid CorridorId) : IEconomyEvent;

/// <summary>Shipment arrived at a hub (intermediate or final).</summary>
public sealed record ShipmentHubArrived(
  SimulationHour Hour,
  Guid ShipmentId,
  FirmId FirmId,
  Guid HubId) : IEconomyEvent;

/// <summary>Fuel taken from hub inventory onto a shipment.</summary>
public sealed record FuelBunkered(
  SimulationHour Hour,
  Guid ShipmentId,
  FirmId FirmId,
  ProductId FuelProductId,
  Quantity Quantity) : IEconomyEvent;

/// <summary>Corridor toll paid from firm cash.</summary>
public sealed record TransportTollPaid(
  SimulationHour Hour,
  Guid ShipmentId,
  FirmId FirmId,
  Money Amount) : IEconomyEvent;

/// <summary>Multi-leg plan could not be formed or departed.</summary>
public sealed record ShipmentPlanFailed(
  SimulationHour Hour,
  FirmId FirmId,
  ProductId ProductId,
  string Reason) : IEconomyEvent;

/// <summary>Observed market trade.</summary>
public sealed record MarketTradeObserved(
  SimulationHour Hour,
  ProductId ProductId,
  Quantity Quantity,
  Money UnitPrice) : IEconomyEvent;

/// <summary>Goods spoiled and were written off.</summary>
public sealed record InventorySpoiled(
  SimulationHour Hour,
  FirmId FirmId,
  ProductId ProductId,
  Quantity Quantity) : IEconomyEvent;

/// <summary>Procurement filled from exogenous supply.</summary>
public sealed record ProcurementFilled(
  SimulationHour Hour,
  FirmId FirmId,
  ProductId ProductId,
  Quantity Quantity,
  Money UnitPrice) : IEconomyEvent;
