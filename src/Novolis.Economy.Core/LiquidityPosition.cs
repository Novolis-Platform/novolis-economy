namespace Novolis.Economy.Core;

/// <summary>Derived ability to meet due obligations (SPEC §14).</summary>
public sealed record LiquidityPosition(
  Money Cash,
  Money AccessibleDeposits,
  Money UndrawnCommittedCredit,
  Money DueNow)
{
  public Money Available => Cash + AccessibleDeposits + UndrawnCommittedCredit;
  public Money Surplus => Available - DueNow;
}