namespace Novolis.Economy.Accounting.Extensions;

/// <summary>One trial-balance row (storage / signed ledger balance).</summary>
public sealed record TrialBalanceLine(AccountRole Role, Money StorageBalance);
