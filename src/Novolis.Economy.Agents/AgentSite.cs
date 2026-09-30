using Novolis.Economy;
using Novolis.Economy.Logistics;

using System.Reflection;
using Novolis.Economy.Abstractions;
using Novolis.Economy.Production;

namespace Novolis.Economy.Agents;

/// <summary>Inventory location + optional facility / hub binding for agent policies.</summary>
public sealed record AgentSite(
  InventoryLocationId LocationId,
  FacilityId? FacilityId = null,
  TransportHubId? HubId = null,
  string Name = "");
