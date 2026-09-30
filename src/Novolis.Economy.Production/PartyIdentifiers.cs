namespace Novolis.Economy;

/// <summary>Ops firm key. Same Guid space as Core <c>LegalEntityId</c>.</summary>
public readonly record struct FirmId(Guid Value)
{
  public static FirmId New() => new(Guid.NewGuid());
  public static FirmId From(Guid value) => new(value);
  public Core.LegalEntityId AsCore() => Core.LegalEntityId.From(Value);
  public static FirmId From(Core.LegalEntityId id) => new(id.Value);
  public override string ToString() => Value.ToString("N");
}
