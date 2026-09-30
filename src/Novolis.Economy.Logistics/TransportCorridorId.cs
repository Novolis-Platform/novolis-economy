namespace Novolis.Economy;

public readonly record struct TransportCorridorId(Guid Value)
{
  public static TransportCorridorId New() => new(Guid.NewGuid());
  public static TransportCorridorId From(Guid value) => new(value);
  public override string ToString() => Value.ToString("N");
}
