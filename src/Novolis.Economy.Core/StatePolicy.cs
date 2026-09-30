namespace Novolis.Economy.Core;

/// <summary>State rules that alter flows (SPEC §17).</summary>
public sealed record StatePolicy(
  decimal HouseholdTaxRate,
  decimal FirmTaxRate,
  Money TransferPerHousehold,
  decimal DepositReserveRequirement,
  decimal InsuranceCapitalRequirement,
  Money WagePerLaborHour)
{
  /// <summary>Zero rates / transfers.</summary>
  public static StatePolicy Neutral { get; } = new(0m, 0m, Money.Zero, 0m, 0m, Money.From(1m));
}