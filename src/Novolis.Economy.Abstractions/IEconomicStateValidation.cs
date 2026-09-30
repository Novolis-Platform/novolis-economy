namespace Novolis.Economy.Abstractions;

/// <summary>
/// Optional diagnostic boundary implemented by model states that can expose
/// Core invariant checks without making Abstractions depend on Core.
/// </summary>
public interface IEconomicStateValidation
{
    IReadOnlyList<EconomicValidationSignal> ValidateState();
}
