using System.Text.Json;
using Novolis.Economy.Abstractions;

namespace Novolis.Economy.Simulation;

/// <summary>Stable format marker for compact run records.</summary>
public static class EconomicRunPersistenceSchema
{
    /// <summary>First stable run-record schema.</summary>
    public const string CurrentFormatVersion = "economy.run.v1";
}
