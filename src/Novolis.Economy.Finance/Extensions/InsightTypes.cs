namespace Novolis.Economy.Finance.Extensions;

/// <summary>Per-loan insight.</summary>
public sealed record LoanInsight(
    LoanId Id,
    FirmId LenderFirmId,
    FirmId BorrowerFirmId,
    LoanStatus Status,
    Money PrincipalRemaining,
    Money AccruedInterest,
    decimal AnnualInterestRate,
    SimulationHour OriginatedAt,
    SimulationHour DueAt);
