namespace Novolis.Economy;

public readonly record struct FacilityId(Guid Value)
{
  public static FacilityId New() => new(Guid.NewGuid());
  public static FacilityId From(Guid value) => new(value);
  public override string ToString() => Value.ToString("N");
}

public readonly record struct BrandId(Guid Value)
{
  public static BrandId New() => new(Guid.NewGuid());
  public override string ToString() => Value.ToString("N");
}

public readonly record struct OperatingUnitId(Guid Value)
{
  public static OperatingUnitId New() => new(Guid.NewGuid());
  public static OperatingUnitId From(Guid value) => new(value);
  public override string ToString() => Value.ToString("N");
}

public readonly record struct ProductionProcessId(Guid Value)
{
  public static ProductionProcessId New() => new(Guid.NewGuid());
  public static ProductionProcessId From(Guid value) => new(value);
  public override string ToString() => Value.ToString("N");
}

public readonly record struct ProductCategoryId(Guid Value)
{
  public static ProductCategoryId New() => new(Guid.NewGuid());
  public static ProductCategoryId From(Guid value) => new(value);
  public override string ToString() => Value.ToString("N");
}

public readonly record struct InventoryLocationId(Guid Value)
{
  public static InventoryLocationId New() => new(Guid.NewGuid());
  public static InventoryLocationId From(Guid value) => new(value);
  public override string ToString() => Value.ToString("N");
}

/// <summary>Legacy freight route id (command surface; hubs preferred).</summary>
public readonly record struct FreightRouteId(Guid Value)
{
  public static FreightRouteId New() => new(Guid.NewGuid());
  public static FreightRouteId From(Guid value) => new(value);
  public override string ToString() => Value.ToString("N");
}

public readonly record struct ShipmentId(Guid Value)
{
  public static ShipmentId New() => new(Guid.NewGuid());
  public static ShipmentId From(Guid value) => new(value);
  public override string ToString() => Value.ToString("N");
}
