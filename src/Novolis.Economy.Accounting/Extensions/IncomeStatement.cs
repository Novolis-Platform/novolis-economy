namespace Novolis.Economy.Accounting.Extensions;

/// <summary>Income statement in presentation amounts (revenue positive, expenses positive).</summary>
public sealed record IncomeStatement(
    Money Revenue,
    Money InterestIncome,
    Money CostOfGoodsSold,
    Money WageExpense,
    Money TransportFuelExpense,
    Money TransportTollExpense,
    Money InterestExpense,
    Money NetIncome);
