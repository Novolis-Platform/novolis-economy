namespace Novolis.Economy;

public readonly record struct TransportHubId(Guid Value)
{
  public static TransportHubId New() => new(Guid.NewGuid());
  public static TransportHubId From(Guid value) => new(value);
  public override string ToString() => Value.ToString("N");
}
