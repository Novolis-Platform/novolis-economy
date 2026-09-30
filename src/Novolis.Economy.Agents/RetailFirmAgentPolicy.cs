using Novolis.Economy;
using Novolis.Economy.Markets;
using Novolis.Economy.Production;

namespace Novolis.Economy.Agents;

/// <summary>Thresholds for retail + bunker sites.</summary>
public sealed record RetailFirmAgentPolicy(
  IReadOnlyList<AgentSite> RetailSites,
  IReadOnlyList<AgentSite> BunkerSites,
  IReadOnlyList<RetailSkuPolicy> RetailSkus,
  BunkerSkuPolicy? Bunker,
  decimal PriceJitter = 0.04m);
