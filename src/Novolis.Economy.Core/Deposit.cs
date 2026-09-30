namespace Novolis.Economy.Core;

/// <summary>Deposit claim against a bank (SPEC §15).</summary>
public sealed record Deposit(
  LegalEntityId Depositor,
  LegalEntityId Bank,
  Money Balance);