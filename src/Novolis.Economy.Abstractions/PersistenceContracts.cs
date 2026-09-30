using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Novolis.Economy.Abstractions;

/// <summary>Version marker for persisted economic model documents.</summary>
public static class EconomicPersistenceSchema
{
    /// <summary>First stable JSON snapshot schema.</summary>
    public const string CurrentFormatVersion = "economy.snapshot.v1";
}
