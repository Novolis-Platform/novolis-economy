using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Originate a term loan from lender cash to borrower cash.</summary>
public sealed record OriginateLoan(
  FirmId LenderFirmId,
  FirmId BorrowerFirmId,
  Money Principal,
  decimal AnnualInterestRate,
  long TermHours) : IEconomyCommand;
