using Novolis.Economy.Core.Transactions;
using Novolis.Economy.Primitives;

namespace Novolis.Economy.Core.Invariants;

/// <summary>
/// Checks the additional structural guarantees required before an
/// <see cref="EconomyState"/> is persisted as a resumable snapshot.
/// </summary>
public static class PersistenceInvariantChecker
{
    /// <summary>
    /// Validates the authority and its append-only journal for persistence.
    /// </summary>
    public static void AssertRestorable(EconomyState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        InvariantChecker.AssertAll(state);

        var seenTransactions = new HashSet<TransactionId>();
        foreach (var transaction in state.Journal)
        {
            if (transaction.Id == default)
            {
                throw new InvalidDataException(
                    "The economic journal contains a transaction without an id.");
            }

            if (!seenTransactions.Add(transaction.Id))
            {
                throw new InvalidDataException(
                    $"The economic journal contains duplicate transaction '{transaction.Id}'.");
            }

            if (transaction.Period is < 0 ||
                transaction.Period > state.Period)
            {
                throw new InvalidDataException(
                    $"Transaction '{transaction.Id}' has period {transaction.Period}, " +
                    $"outside the authority period {state.Period}.");
            }

            foreach (var effect in transaction.Effects)
            {
                ValidateEffect(state, effect);
            }
        }

        if (state.TransitionSequence < state.Journal.Count)
        {
            throw new InvalidDataException(
                "The authority transition sequence precedes its journal length.");
        }
    }

    private static void ValidateEffect(
        EconomyState state,
        EconomicEffect effect)
    {
        switch (effect)
        {
            case PositionChange change:
                state.ValidatePositionChange(
                    change.Owner,
                    change.Asset,
                    change.Region);
                break;
            case CreateClaim create:
                state.ValidatePositionChange(
                    create.Claim.Creditor,
                    create.Claim.Principal.Asset,
                    region: null);
                state.ValidatePositionChange(
                    create.Claim.Debtor,
                    create.Claim.Principal.Asset,
                    region: null);
                if (create.Claim.Principal.Quantity < 0m ||
                    create.Claim.RemainingPeriods < 0)
                {
                    throw new InvalidDataException(
                        $"Claim '{create.Claim.Id}' has invalid terms.");
                }

                break;
            case SettleClaim settle:
                if (settle.Amount.Quantity < 0m ||
                    !state.ClaimState.ContainsKey(settle.ClaimId))
                {
                    throw new InvalidDataException(
                        $"Claim settlement '{settle.ClaimId}' is not valid.");
                }

                break;
            default:
                throw new InvalidDataException(
                    $"The economic journal contains unsupported effect " +
                    $"'{effect.GetType().Name}'.");
        }
    }
}
