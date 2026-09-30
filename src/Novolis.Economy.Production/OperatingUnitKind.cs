using System.Collections.Immutable;
using Novolis.Economy;

namespace Novolis.Economy.Production;

/// <summary>Kind of operating unit in a facility.</summary>
public enum OperatingUnitKind
{
  /// <summary>Purchasing.</summary>
  Purchasing = 0,
  /// <summary>Storage.</summary>
  Storage = 1,
  /// <summary>Manufacturing.</summary>
  Manufacturing = 2,
  /// <summary>Assembly.</summary>
  Assembly = 3,
  /// <summary>Quality assurance.</summary>
  QualityAssurance = 4,
  /// <summary>Sales / retail.</summary>
  Sales = 5,
  /// <summary>Advertising.</summary>
  Advertising = 6,
  /// <summary>Research.</summary>
  Research = 7,
  /// <summary>Training.</summary>
  Training = 8,
  /// <summary>Dispatch / shipping.</summary>
  Dispatch = 9,
}
