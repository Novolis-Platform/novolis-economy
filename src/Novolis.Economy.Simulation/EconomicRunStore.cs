using System.Text.Json;
using Novolis.Economy.Abstractions;

namespace Novolis.Economy.Simulation;

/// <summary>Stable format marker for compact run records.</summary>
public static class EconomicRunPersistenceSchema
{
    /// <summary>First stable run-record schema.</summary>
    public const string CurrentFormatVersion = "economy.run.v1";
}

/// <summary>
/// Data-only run record. It deliberately omits the opaque model state; a
/// resumable state belongs in an economic snapshot.
/// </summary>
public sealed record EconomicRunRecord(
    string FormatVersion,
    EconomicModelRunManifest Manifest,
    IReadOnlyList<EconomicObservation> Observations,
    IReadOnlyList<EconomicTransitionReceipt> Transactions);

/// <summary>Canonical JSON import/export for runs and validation reports.</summary>
public static class EconomicRunStore
{
    /// <summary>Converts an executed run to a versioned data record.</summary>
    public static EconomicRunRecord Capture(EconomicModelRunResult run)
    {
        ArgumentNullException.ThrowIfNull(run);
        return new EconomicRunRecord(
            EconomicRunPersistenceSchema.CurrentFormatVersion,
            run.Manifest,
            run.Observations,
            run.Transactions);
    }

    /// <summary>Serializes a compact run record as canonical JSON.</summary>
    public static string Serialize(EconomicModelRunResult run) =>
        Serialize(Capture(run));

    /// <summary>Serializes a run record as canonical JSON.</summary>
    public static string Serialize(EconomicRunRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        RequireCurrentSchema(record);
        return EconomicJson.SerializeCanonical(
            record,
            EconomicJson.CreateOptions());
    }

    /// <summary>Reads a compact run record from JSON.</summary>
    public static EconomicRunRecord Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var record = JsonSerializer.Deserialize<EconomicRunRecord>(
            json,
            EconomicJson.CreateOptions())
            ?? throw new InvalidDataException("The economic run JSON was empty.");
        RequireCurrentSchema(record);
        return record;
    }

    /// <summary>Serializes a validation report for later inspection.</summary>
    public static string Serialize(EconomicValidationReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return EconomicJson.SerializeCanonical(
            report,
            EconomicJson.CreateOptions());
    }

    /// <summary>Reads a validation report from JSON.</summary>
    public static EconomicValidationReport DeserializeValidationReport(
        string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        return JsonSerializer.Deserialize<EconomicValidationReport>(
                   json,
                   EconomicJson.CreateOptions())
               ?? throw new InvalidDataException(
                   "The validation report JSON was empty.");
    }

    private static void RequireCurrentSchema(EconomicRunRecord record)
    {
        if (!string.Equals(
                record.FormatVersion,
                EconomicRunPersistenceSchema.CurrentFormatVersion,
                StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Unsupported economic run format '{record.FormatVersion}'.");
        }

        if (record.Observations is null || record.Transactions is null)
        {
            throw new InvalidDataException(
                "Economic run collections cannot be null.");
        }
    }
}
