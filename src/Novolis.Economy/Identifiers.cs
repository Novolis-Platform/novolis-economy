namespace Novolis.Economy;

/// <summary>Identifies a firm (company).</summary>
/// <param name="Value">Opaque firm key.</param>
public readonly record struct FirmId(Guid Value)
{
  /// <summary>Creates a new random firm id.</summary>
  public static FirmId New() => new(Guid.NewGuid());

  /// <inheritdoc />
  public override string ToString() => Value.ToString("N");
}

/// <summary>Identifies a facility (plant, warehouse, store).</summary>
/// <param name="Value">Opaque facility key.</param>
public readonly record struct FacilityId(Guid Value)
{
  /// <summary>Creates a new random facility id.</summary>
  public static FacilityId New() => new(Guid.NewGuid());

  /// <inheritdoc />
  public override string ToString() => Value.ToString("N");
}

/// <summary>Identifies a product definition.</summary>
/// <param name="Value">Opaque product key.</param>
public readonly record struct ProductId(Guid Value)
{
  /// <summary>Creates a new random product id.</summary>
  public static ProductId New() => new(Guid.NewGuid());

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

  /// <inheritdoc />
  public override string ToString() => Value.ToString("N");
}

/// <summary>Identifies a geographic area.</summary>
/// <param name="Value">Opaque area key.</param>
public readonly record struct GeographicAreaId(Guid Value)
{
  /// <summary>Creates a new random area id.</summary>
  public static GeographicAreaId New() => new(Guid.NewGuid());

  /// <inheritdoc />
  public override string ToString() => Value.ToString("N");
}

/// <summary>Identifies an operating unit inside a facility layout.</summary>
/// <param name="Value">Opaque unit key.</param>
public readonly record struct OperatingUnitId(Guid Value)
{
  /// <summary>Creates a new random operating unit id.</summary>
  public static OperatingUnitId New() => new(Guid.NewGuid());

  /// <inheritdoc />
  public override string ToString() => Value.ToString("N");
}

/// <summary>Identifies a production process / recipe family.</summary>
/// <param name="Value">Opaque process key.</param>
public readonly record struct ProductionProcessId(Guid Value)
{
  /// <summary>Creates a new random process id.</summary>
  public static ProductionProcessId New() => new(Guid.NewGuid());

  /// <inheritdoc />
  public override string ToString() => Value.ToString("N");
}

/// <summary>Identifies a product category.</summary>
/// <param name="Value">Opaque category key.</param>
public readonly record struct ProductCategoryId(Guid Value)
{
  /// <summary>Creates a new random category id.</summary>
  public static ProductCategoryId New() => new(Guid.NewGuid());

  /// <inheritdoc />
  public override string ToString() => Value.ToString("N");
}
