using SmartBudget.WinForms.Persistence.Sqlite.Repositories;
namespace SmartBudget.WinForms.Services;

public sealed class CalendarDataService
{
    private readonly IAccountRepository _accounts;
    private readonly ITransactionRepository _transactions;

    public CalendarDataService(IAccountRepository accounts, ITransactionRepository transactions)
    {
        _accounts = accounts;
        _transactions = transactions;
    }

    public async Task<double> GetPreviousBalance(long accountId, int year, int month, CancellationToken ct)
    {
        var monthStart = new DateOnly(year, month, 1);
        var account = await _accounts.GetByIdAsync(accountId, ct);
        var txns = await _transactions.GetSumBeforeDateAsync(accountId, monthStart, ct);

        return txns + account.OpeningBalanceCents;
    }

    public async Task<IReadOnlyList<CalendarDayData>> GetMonthAsync(long accountId, int year, int month, CancellationToken ct)
    {
        var monthStart = new DateOnly(year, month, 1);
        var monthEnd = monthStart.AddMonths(1);

        var account = await _accounts.GetByIdAsync(accountId, ct)
            ?? throw new InvalidOperationException($"Account {accountId} not found.");

        var sumBefore = await _transactions.GetSumBeforeDateAsync(accountId, monthStart, ct);

        var running = account.OpeningBalanceCents + sumBefore;

        var txns = await _transactions.GetForRangeAsync(accountId, monthStart, monthEnd, ct);

        var byDay = txns.GroupBy(t => t.Date).ToDictionary(g => g.Key, g => (IReadOnlyList<TransactionRow>)g.ToList());

        var daysInMonth = DateTime.DaysInMonth(year, month);
        var result = new List<CalendarDayData>(daysInMonth);

        for (int d = 1; d <= daysInMonth; d++)
        {
            var date = new DateOnly(year, month, d);

            if (!byDay.TryGetValue(date, out var dayTxns))
                dayTxns = Array.Empty<TransactionRow>();

            long deposits = 0, charges = 0, total = 0;

            foreach (var t in dayTxns)
            {
                total += t.AmountCents;
                if (t.AmountCents >= 0) deposits += t.AmountCents;
                else charges += -t.AmountCents;
            }

            running += total;

            result.Add(new CalendarDayData(
                date,
                deposits,
                charges,
                total,
                running,
                dayTxns));
        }

        return result;
    }
}
