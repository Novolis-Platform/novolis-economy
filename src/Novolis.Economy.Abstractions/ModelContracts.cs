using Novolis.Economy.Primitives;

namespace Novolis.Economy.Abstractions;

/// <summary>Explicit treatment of monetary flows that do not close domestically.</summary>
public enum MonetaryClosure
{
    Open = 0,
    Closed = 1,
    ExternalSector = 2
}
