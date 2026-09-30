using System.Collections.Immutable;
using Novolis.Economy;
using Novolis.Economy.Agents;
using Novolis.Economy.Accounting;
using Novolis.Economy.Core;
using Novolis.Economy.Core.Finance;
using Novolis.Economy.Logistics;
using Novolis.Economy.Markets;
using Novolis.Economy.Population;
using Novolis.Economy.Production;

namespace Novolis.Economy.Simulation;

/// <summary>Controls whether integration code may create missing Core facts.</summary>
public enum RegistrationMode
{
  /// <summary>Missing entities, assets, and regions are errors.</summary>
  Strict = 0,

  /// <summary>Legacy game integration may create generic facts explicitly.</summary>
  Implicit = 1,
}
