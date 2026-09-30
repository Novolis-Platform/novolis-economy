using Novolis.Economy;

namespace Novolis.Economy.Logistics;

/// <summary>Well-known logistics product ids (not catalog SKUs).</summary>
public static class LogisticsProductIds
{
  /// <summary>Empty-hold marker for <see cref="PlanReposition"/> shipments.</summary>
  public static ProductId EmptyHold { get; } =
    ProductId.From(Guid.Parse("00000000-0000-4000-8000-00000000e001"));
}
