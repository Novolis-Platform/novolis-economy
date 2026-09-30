using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Issue a shipment along a freight route.</summary>
public sealed record IssueShipment(
  FirmId FirmId,
  FreightRouteId RouteId,
  ProductId ProductId,
  Quantity Quantity) : IEconomyCommand;
