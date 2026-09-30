using System.Collections.Immutable;
using Novolis.Economy;
using Novolis.Economy.Abstractions;

namespace Novolis.Economy.Accounting;

/// <summary>Identifies a ledger account.</summary>
public readonly record struct AccountId(Guid Value)
{
  /// <summary>Creates a new account id.</summary>
  public static AccountId New() => new(Guid.NewGuid());

  /// <summary>Creates an account id from a fixed guid.</summary>
  public static AccountId From(Guid value) => new(value);

  /// <inheritdoc />
  public override string ToString() => Value.ToString("N");
}
