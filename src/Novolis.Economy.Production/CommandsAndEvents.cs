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

/// <summary>Sell inventory into the exogenous export market (infinite demand at the stated unit price floor).</summary>
public sealed record PlaceExportOrder(
  FirmId SellerFirmId,
  InventoryLocationId Origin,
  ProductId ProductId,
  Quantity Quantity,
  Money MinUnitPrice) : IEconomyCommand;

/// <summary>Issue a shipment along a freight route.</summary>
public sealed record IssueShipment(
  FirmId FirmId,
  FreightRouteId RouteId,
  ProductId ProductId,
  Quantity Quantity) : IEconomyCommand;

/// <summary>Plan and depart a multi-leg shipment between transport hubs.
/// <paramref name="TransitProfileCode"/> is Logistics TransitProfile ordinal:
/// 0=SlowEconomic, 1=StandardCommercial, 2=PriorityCommercial.
/// </summary>
public sealed record PlanShipment(
  FirmId FirmId,
  Guid OriginHubId,
  Guid DestinationHubId,
  ProductId ProductId,
  Quantity Quantity,
  Guid VehicleClassId,
  int TransitProfileCode = 1) : IEconomyCommand;

/// <summary>
/// Empty-hull reposition between hubs (no cargo). Still burns fuel, tolls, and drive wear.
/// <paramref name="TransitProfileCode"/> matches <see cref="PlanShipment"/>.
/// </summary>
public sealed record PlanReposition(
  FirmId FirmId,
  Guid OriginHubId,
  Guid DestinationHubId,
  Guid VehicleClassId,
  int TransitProfileCode = 1) : IEconomyCommand;

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

/// <summary>Export filled into exogenous demand (inventory removed; cash credited).</summary>
public sealed record ExportFilled(
  SimulationHour Hour,
  FirmId FirmId,
  ProductId ProductId,
  Quantity Quantity,
  Money UnitPrice,
  Money Revenue) : IEconomyEvent;

/// <summary>Sell inventory from one firm to another for cash at a shared location.</summary>
public sealed record TransferGoodsForCash(
  FirmId SellerFirmId,
  FirmId BuyerFirmId,
  InventoryLocationId LocationId,
  ProductId ProductId,
  Quantity Quantity,
  Money UnitPrice) : IEconomyCommand;

/// <summary>Inter-firm goods sale completed (inventory moved; cash posted).</summary>
public sealed record GoodsSoldInterFirm(
  SimulationHour Hour,
  FirmId SellerFirmId,
  FirmId BuyerFirmId,
  InventoryLocationId LocationId,
  ProductId ProductId,
  Quantity Quantity,
  Money UnitPrice,
  Money Revenue) : IEconomyEvent;

/// <summary>Inter-firm goods transfer could not complete.</summary>
public sealed record TransferGoodsFailed(
  SimulationHour Hour,
  FirmId SellerFirmId,
  FirmId BuyerFirmId,
  ProductId ProductId,
  string Reason) : IEconomyEvent;

/// <summary>Wage cash redistributed to household cohort budgets.</summary>
public sealed record HouseholdCreditsIssued(
  SimulationHour Hour,
  FirmId FirmId,
  Money Amount) : IEconomyEvent;

/// <summary>Buy or sell side of a hub spot order.</summary>
public enum HubOrderSide
{
  /// <summary>Bid to buy.</summary>
  Buy = 0,
  /// <summary>Offer to sell.</summary>
  Sell = 1,
}

/// <summary>Post a limit order at a hub inventory location.</summary>
public sealed record PostHubOrder(
  FirmId FirmId,
  InventoryLocationId LocationId,
  ProductId ProductId,
  HubOrderSide Side,
  Quantity Quantity,
  Money LimitPrice) : IEconomyCommand;

/// <summary>Cancel an open hub order.</summary>
public sealed record CancelHubOrder(Guid OrderId) : IEconomyCommand;

/// <summary>Hub order accepted onto the book.</summary>
public sealed record HubOrderPosted(
  SimulationHour Hour,
  Guid OrderId,
  FirmId FirmId,
  InventoryLocationId LocationId,
  ProductId ProductId,
  HubOrderSide Side,
  Quantity Quantity,
  Money LimitPrice) : IEconomyEvent;

/// <summary>Hub order (partially) filled against a counterparty.</summary>
public sealed record HubOrderFilled(
  SimulationHour Hour,
  Guid BuyOrderId,
  Guid SellOrderId,
  FirmId BuyerFirmId,
  FirmId SellerFirmId,
  InventoryLocationId LocationId,
  ProductId ProductId,
  Quantity Quantity,
  Money UnitPrice) : IEconomyEvent;

/// <summary>Hub order removed from the book.</summary>
public sealed record HubOrderCancelled(
  SimulationHour Hour,
  Guid OrderId) : IEconomyEvent;

/// <summary>Originate a term loan from lender cash to borrower cash.</summary>
public sealed record OriginateLoan(
  FirmId LenderFirmId,
  FirmId BorrowerFirmId,
  Money Principal,
  decimal AnnualInterestRate,
  long TermHours) : IEconomyCommand;

/// <summary>Repay principal (and accrued interest) on a loan up to the given amount.</summary>
public sealed record RepayLoan(
  LoanId LoanId,
  Money Amount) : IEconomyCommand;

/// <summary>Loan funds disbursed.</summary>
public sealed record LoanOriginated(
  SimulationHour Hour,
  LoanId LoanId,
  FirmId LenderFirmId,
  FirmId BorrowerFirmId,
  Money Principal,
  decimal AnnualInterestRate,
  SimulationHour DueAt) : IEconomyEvent;

/// <summary>Interest added to loan balance.</summary>
public sealed record InterestAccrued(
  SimulationHour Hour,
  LoanId LoanId,
  Money Amount) : IEconomyEvent;

/// <summary>Cash repayment applied to a loan.</summary>
public sealed record LoanRepaid(
  SimulationHour Hour,
  LoanId LoanId,
  Money Amount,
  Money PrincipalRemaining) : IEconomyEvent;

/// <summary>Borrower missed a required repayment.</summary>
public sealed record LoanDefaulted(
  SimulationHour Hour,
  LoanId LoanId,
  FirmId BorrowerFirmId,
  Money PrincipalRemaining) : IEconomyEvent;

/// <summary>Set an absolute ownership fraction on an issuer.</summary>
public sealed record AssignOwnership(
  FirmId IssuerFirmId,
  FirmId OwnerFirmId,
  decimal Fraction) : IEconomyCommand;

/// <summary>Move ownership fraction between owners of the same issuer.</summary>
public sealed record TransferOwnership(
  FirmId IssuerFirmId,
  FirmId FromOwnerFirmId,
  FirmId ToOwnerFirmId,
  decimal Fraction) : IEconomyCommand;

/// <summary>Pay a cash dividend from issuer to claim holders (pro-rata).</summary>
public sealed record DeclareDividend(
  FirmId IssuerFirmId,
  Money Total) : IEconomyCommand;

/// <summary>Ownership claim changed.</summary>
public sealed record OwnershipChanged(
  SimulationHour Hour,
  FirmId IssuerFirmId,
  FirmId OwnerFirmId,
  decimal Fraction) : IEconomyEvent;

/// <summary>Dividend cash paid to one owner.</summary>
public sealed record DividendPaid(
  SimulationHour Hour,
  FirmId IssuerFirmId,
  FirmId OwnerFirmId,
  Money Amount) : IEconomyEvent;

/// <summary>Spend cash to scale facility manufacturing/assembly capacity.</summary>
public sealed record UpgradeFacility(
  FacilityId FacilityId,
  Money Cost,
  decimal CapacityFactor) : IEconomyCommand;

/// <summary>Facility capacity increased after cash investment.</summary>
public sealed record FacilityUpgraded(
  SimulationHour Hour,
  FacilityId FacilityId,
  FirmId OwnerFirmId,
  Money Cost,
  decimal CapacityFactor,
  Quantity ManufacturingCapacity) : IEconomyEvent;

/// <summary>Facility upgrade rejected (usually insufficient cash).</summary>
public sealed record FacilityUpgradeFailed(
  SimulationHour Hour,
  FacilityId FacilityId,
  string Reason) : IEconomyEvent;

/// <summary>Borrower credit frozen after default.</summary>
public sealed record CreditFrozenSet(
  SimulationHour Hour,
  FirmId FirmId) : IEconomyEvent;

/// <summary>Facility ownership rebinding after default absorb.</summary>
public sealed record FacilityAbsorbed(
  SimulationHour Hour,
  FacilityId FacilityId,
  FirmId FromFirmId,
  FirmId ToFirmId) : IEconomyEvent;

/// <summary>Pay cash for an ownership fraction (households debit BudgetRemaining).</summary>
public sealed record PurchaseOwnership(
  FirmId IssuerFirmId,
  FirmId BuyerFirmId,
  decimal Fraction,
  Money Price) : IEconomyCommand;

/// <summary>Ownership purchased for cash/budget.</summary>
public sealed record OwnershipPurchased(
  SimulationHour Hour,
  FirmId IssuerFirmId,
  FirmId BuyerFirmId,
  decimal Fraction,
  Money Price) : IEconomyEvent;
