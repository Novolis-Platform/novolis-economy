namespace Novolis.Economy.Core.Transactions;

/// <summary>Create or originate an authoritative financial claim.</summary>
public sealed record CreateClaim(FinancialClaim Claim) : EconomicEffect;