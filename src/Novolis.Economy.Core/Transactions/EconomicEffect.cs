using System.Text.Json.Serialization;

namespace Novolis.Economy.Core.Transactions;

/// <summary>Base type for small, state-changing economic effects.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(PositionChange), "position-change")]
[JsonDerivedType(typeof(CreateClaim), "create-claim")]
[JsonDerivedType(typeof(SettleClaim), "settle-claim")]
public abstract record EconomicEffect;