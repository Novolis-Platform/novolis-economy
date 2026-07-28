using Novolis.Economy;
using Novolis.Economy.Finance;
using Novolis.Economy.Finance.Extensions;
using TUnit.Core;

namespace Novolis.Economy.Unit;

public sealed class FinanceExtensionsTests
{
    [Test]
    public async Task LoanBookSnapshot_CountsByStatus()
    {
        var lender = FirmId.From(Guid.Parse("f1000000-0000-4000-8000-000000000001"));
        var borrower = FirmId.From(Guid.Parse("f1000000-0000-4000-8000-000000000002"));
        var active = new Loan(
            LoanId.New(), lender, borrower, Money.From(100m), 0.1m,
            SimulationHour.Epoch, SimulationHour.Epoch.AddHours(100));
        var closed = new Loan(
            LoanId.New(), lender, borrower, Money.From(50m), 0.1m,
            SimulationHour.Epoch, SimulationHour.Epoch.AddHours(10));
        closed.Status = LoanStatus.Closed;
        closed.PrincipalRemaining = Money.Zero;

        var snap = new[] { active, closed }.Snapshot();
        await Assert.That(snap.ActiveCount).IsEqualTo(1);
        await Assert.That(snap.ClosedCount).IsEqualTo(1);
        await Assert.That(snap.PrincipalOutstanding.Amount).IsEqualTo(100m);
    }
}
