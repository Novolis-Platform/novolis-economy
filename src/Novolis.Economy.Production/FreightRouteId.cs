namespace Novolis.Economy;

/// <summary>Legacy freight route id (command surface; hubs preferred).</summary>
public readonly record struct FreightRouteId(Guid Value)
{
  public static FreightRouteId New() => new(Guid.NewGuid());
  public static FreightRouteId From(Guid value) => new(value);
  public override string ToString() => Value.ToString("N");
}
