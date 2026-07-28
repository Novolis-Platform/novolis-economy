namespace Novolis.Economy;

/// <summary>Identifies a firm (company).</summary>
/// <param name="Value">Opaque firm key.</param>
public readonly record struct FirmId(Guid Value)
{
  /// <summary>Creates a new random firm id.</summary>
  public static FirmId New() => new(Guid.NewGuid());

  /// <summary>Creates a firm id from a fixed guid.</summary>
  public static FirmId From(Guid value) => new(value);

  /// <inheritdoc />
  public override string ToString() => Value.ToString("N");
}

/// <summary>Identifies a facility (plant, warehouse, store).</summary>
/// <param name="Value">Opaque facility key.</param>
public readonly record struct FacilityId(Guid Value)
{
  /// <summary>Creates a new random facility id.</summary>
  public static FacilityId New() => new(Guid.NewGuid());

  /// <summary>Creates a facility id from a fixed guid.</summary>
  public static FacilityId From(Guid value) => new(value);

  /// <inheritdoc />
  public override string ToString() => Value.ToString("N");
}

/// <summary>Identifies a product definition.</summary>
/// <param name="Value">Opaque product key.</param>
public readonly record struct ProductId(Guid Value)
{
  /// <summary>Creates a new random product id.</summary>
  public static ProductId New() => new(Guid.NewGuid());

  /// <summary>Creates a product id from a fixed guid.</summary>
  public static ProductId From(Guid value) => new(value);

  /// <inheritdoc />
  public override string ToString() => Value.ToString("N");
}

/// <summary>Identifies a brand.</summary>
/// <param name="Value">Opaque brand key.</param>
public readonly record struct BrandId(Guid Value)
{
  /// <summary>Creates a new random brand id.</summary>
  public static BrandId New() => new(Guid.NewGuid());

  /// <inheritdoc />
  public override string ToString() => Value.ToString("N");
}

/// <summary>Identifies a consumer cohort.</summary>
/// <param name="Value">Opaque cohort key.</param>
public readonly record struct ConsumerCohortId(Guid Value)
{
  /// <summary>Creates a new random cohort id.</summary>
  public static ConsumerCohortId New() => new(Guid.NewGuid());

  /// <summary>Creates a cohort id from a fixed guid.</summary>
  public static ConsumerCohortId From(Guid value) => new(value);

  /// <inheritdoc />
  public override string ToString() => Value.ToString("N");
}

/// <summary>Identifies a geographic area.</summary>
/// <param name="Value">Opaque area key.</param>
public readonly record struct GeographicAreaId(Guid Value)
{
  /// <summary>Creates a new random area id.</summary>
  public static GeographicAreaId New() => new(Guid.NewGuid());

  /// <summary>Creates an area id from a fixed guid.</summary>
  public static GeographicAreaId From(Guid value) => new(value);

  /// <inheritdoc />
  public override string ToString() => Value.ToString("N");
}

/// <summary>Identifies an operating unit inside a facility layout.</summary>
/// <param name="Value">Opaque unit key.</param>
public readonly record struct OperatingUnitId(Guid Value)
{
  /// <summary>Creates a new random operating unit id.</summary>
  public static OperatingUnitId New() => new(Guid.NewGuid());

  /// <summary>Creates an operating unit id from a fixed guid.</summary>
  public static OperatingUnitId From(Guid value) => new(value);

  /// <inheritdoc />
  public override string ToString() => Value.ToString("N");
}

/// <summary>Identifies a production process / recipe family.</summary>
/// <param name="Value">Opaque process key.</param>
public readonly record struct ProductionProcessId(Guid Value)
{
  /// <summary>Creates a new random process id.</summary>
  public static ProductionProcessId New() => new(Guid.NewGuid());

  /// <summary>Creates a process id from a fixed guid.</summary>
  public static ProductionProcessId From(Guid value) => new(value);

  /// <inheritdoc />
  public override string ToString() => Value.ToString("N");
}

/// <summary>Identifies a product category.</summary>
/// <param name="Value">Opaque category key.</param>
public readonly record struct ProductCategoryId(Guid Value)
{
  /// <summary>Creates a new random category id.</summary>
  public static ProductCategoryId New() => new(Guid.NewGuid());

  /// <summary>Creates a category id from a fixed guid.</summary>
  public static ProductCategoryId From(Guid value) => new(value);

  /// <inheritdoc />
  public override string ToString() => Value.ToString("N");
}

/// <summary>Identifies an inventory location (warehouse bin, vehicle, store shelf).</summary>
/// <param name="Value">Opaque location key.</param>
public readonly record struct InventoryLocationId(Guid Value)
{
  /// <summary>Creates a new location id.</summary>
  public static InventoryLocationId New() => new(Guid.NewGuid());

  /// <summary>Creates a location id from a fixed guid (deterministic scenarios).</summary>
  public static InventoryLocationId From(Guid value) => new(value);

  /// <inheritdoc />
  public override string ToString() => Value.ToString("N");
}

/// <summary>Identifies a freight route.</summary>
/// <param name="Value">Opaque route key.</param>
public readonly record struct FreightRouteId(Guid Value)
{
  /// <summary>Creates a new route id.</summary>
  public static FreightRouteId New() => new(Guid.NewGuid());

  /// <summary>Creates a route id from a fixed guid (deterministic scenarios).</summary>
  public static FreightRouteId From(Guid value) => new(value);

  /// <inheritdoc />
  public override string ToString() => Value.ToString("N");
}

/// <summary>Identifies a shipment in transit.</summary>
/// <param name="Value">Opaque shipment key.</param>
public readonly record struct ShipmentId(Guid Value)
{
  /// <summary>Creates a new shipment id.</summary>
  public static ShipmentId New() => new(Guid.NewGuid());

  /// <summary>Creates a shipment id from a fixed guid (deterministic scenarios).</summary>
  public static ShipmentId From(Guid value) => new(value);

  /// <inheritdoc />
  public override string ToString() => Value.ToString("N");
}

/// <summary>Identifies an inter-firm term loan.</summary>
/// <param name="Value">Opaque loan key.</param>
public readonly record struct LoanId(Guid Value)
{
  /// <summary>Creates a new loan id.</summary>
  public static LoanId New() => new(Guid.NewGuid());

  /// <summary>Creates a loan id from a fixed guid.</summary>
  public static LoanId From(Guid value) => new(value);

  /// <inheritdoc />
  public override string ToString() => Value.ToString("N");
}

/// <summary>Identifies a transport hub (port, yard, starport dock).</summary>
/// <param name="Value">Opaque hub key.</param>
public readonly record struct TransportHubId(Guid Value)
{
  /// <summary>Creates a new hub id.</summary>
  public static TransportHubId New() => new(Guid.NewGuid());

  /// <summary>Creates a hub id from a fixed guid.</summary>
  public static TransportHubId From(Guid value) => new(value);

  /// <inheritdoc />
  public override string ToString() => Value.ToString("N");
}

/// <summary>Identifies a transport corridor (directed leg).</summary>
/// <param name="Value">Opaque corridor key.</param>
public readonly record struct TransportCorridorId(Guid Value)
{
  /// <summary>Creates a new corridor id.</summary>
  public static TransportCorridorId New() => new(Guid.NewGuid());

  /// <summary>Creates a corridor id from a fixed guid.</summary>
  public static TransportCorridorId From(Guid value) => new(value);

  /// <inheritdoc />
  public override string ToString() => Value.ToString("N");
}

/// <summary>Identifies a vehicle class.</summary>
/// <param name="Value">Opaque vehicle-class key.</param>
public readonly record struct VehicleClassId(Guid Value)
{
  /// <summary>Creates a new vehicle class id.</summary>
  public static VehicleClassId New() => new(Guid.NewGuid());

  /// <summary>Creates a vehicle class id from a fixed guid.</summary>
  public static VehicleClassId From(Guid value) => new(value);

  /// <inheritdoc />
  public override string ToString() => Value.ToString("N");
}
