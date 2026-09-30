using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Novolis.Economy.Abstractions;

/// <summary>
/// A command preserved in a snapshot without relying on CLR type names.
/// </summary>
public sealed record EconomicCommandDocument(
    long Tick,
    string Kind,
    JsonElement Payload);
