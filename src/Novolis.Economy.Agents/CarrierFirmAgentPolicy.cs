using Novolis.Economy.Abstractions;
using Novolis.Economy;
using Novolis.Economy.Logistics;
using Novolis.Economy.Markets;
using Novolis.Economy.Production;

namespace Novolis.Economy.Agents;

/// <summary>Thresholds for a tramp / carrier firm.</summary>
public sealed record CarrierFirmAgentPolicy(
  IReadOnlyList<AgentSite> Sites,
  IReadOnlyList<ProductId> FreightProducts,
  ProductId FuelProduct,
  VehicleClassId VehicleClassId,
  VehicleClass Vehicle,
  decimal MinMargin,
  Func<ProductId, decimal> GatePrice,
  decimal FuelBuyLimitPrice,
  decimal MinBunkerFuel = 4m,
  bool AllowFuelProcurement = true,
  Func<ProductId, TransitProfile>? ChooseTransitProfile = null,
  Func<bool>? CanOperate = null,
  Func<TransportHubId, bool>? AvoidHub = null,
  Func<decimal>? EffectiveMinMargin = null,
  Func<ProductId, TransportHubId, TransportHubId, TransitProfile, bool>? RefuseHaul = null);
