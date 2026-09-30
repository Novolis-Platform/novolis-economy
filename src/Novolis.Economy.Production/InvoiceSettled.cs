using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Invoice cash settlement.</summary>
public sealed record InvoiceSettled(
  SimulationHour Hour,
  Guid InvoiceId,
  Money AmountPaid) : IEconomyEvent;
