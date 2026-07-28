namespace Novolis.Economy;

public readonly record struct TransportHubId(Guid Value)
{
  public static TransportHubId New() => new(Guid.NewGuid());
  public static TransportHubId From(Guid value) => new(value);
  public override string ToString() => Value.ToString("N");
}

public readonly record struct TransportCorridorId(Guid Value)
{
  public static TransportCorridorId New() => new(Guid.NewGuid());
  public static TransportCorridorId From(Guid value) => new(value);
  public override string ToString() => Value.ToString("N");
}

public readonly record struct VehicleClassId(Guid Value)
{
  public static VehicleClassId New() => new(Guid.NewGuid());
  public static VehicleClassId From(Guid value) => new(value);
  public override string ToString() => Value.ToString("N");
}
