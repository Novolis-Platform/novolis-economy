using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Buy or sell side of a hub spot order.</summary>
public enum HubOrderSide
{
  /// <summary>Bid to buy.</summary>
  Buy = 0,
  /// <summary>Offer to sell.</summary>
  Sell = 1,
}
