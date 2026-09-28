using System.Text;

namespace Novolis.Economy;

/// <summary>Process-stable hashing for deterministic economic fingerprints.</summary>
public static class StableIdentityHash
{
  /// <summary>FNV-1a hash of a Guid's byte representation.</summary>
  public static ulong Guid(Guid value)
  {
    const ulong offset = 14695981039346656037UL;
    const ulong prime = 1099511628211UL;
    var hash = offset;
    foreach (var b in value.ToByteArray())
      hash = (hash ^ b) * prime;
    return hash;
  }

  /// <summary>FNV-1a hash of UTF-8 text.</summary>
  public static ulong Text(string value)
  {
    ArgumentNullException.ThrowIfNull(value);
    const ulong offset = 14695981039346656037UL;
    const ulong prime = 1099511628211UL;
    var hash = offset;
    foreach (var b in Encoding.UTF8.GetBytes(value))
      hash = (hash ^ b) * prime;
    return hash;
  }
}
