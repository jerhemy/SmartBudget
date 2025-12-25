using Dapper;
using SmartBudget.Recurring;
using SmartBudget.Infrastructure.Persistence.Sqlite;
using System.Globalization;


namespace SmartBudget.WinForms.Persistence.Sqlite.Repositories;


public sealed record MonthlyBalancePoint(
    DateOnly MonthStart,
    DateOnly MonthEnd,
    long EndBalanceCents
);

public sealed class Row
{
    public long Id { get; init; }
    public long AccountId { get; init; }
    public string TxnDate { get; init; } = "";
    public string? Title { get; init; }
    public double AmountCents { get; init; }
}

public sealed class SummaryRepository
{
    private readonly SqliteConnectionFactory _factory;

    public SummaryRepository(SqliteConnectionFactory factory) => _factory = factory;


    public async Task<IReadOnlyList<AutoPayTxn>> GetForDetectionAsync(long accountId,int lookbackMonths,CancellationToken ct)
    {

        const string sql = """
        SELECT
            t.id              AS Id,
            t.account_id      AS AccountId,
            t.txn_date        AS TxnDate,
            t.description     AS Title,
            CAST(t.amount_cents AS INTEGER) AS AmountCents
        FROM transactions t
        WHERE t.account_id = @accountId
            AND t.txn_date >= date('now', '-' || @lookbackMonths || ' months')
        ORDER BY t.txn_date ASC;
        """;

        using var conn = _factory.CreateOpenConnection();

        var rows = await conn.QueryAsync<Row>(
            new CommandDefinition(
                sql,
                new { accountId, lookbackMonths },
                cancellationToken: ct));

        var list = new List<AutoPayTxn>();

        foreach (var r in rows)
        {
            // txn_date stored as YYYY-MM-DD
            if (!DateOnly.TryParseExact(r.TxnDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                continue; // or throw if you prefer strictness

            list.Add(new AutoPayTxn(
                Id: r.Id,
                AccountId: r.AccountId,
                Date: date,
                Title: r.Title ?? string.Empty,
                AmountCents: (long)r.AmountCents));
        }

        return list;

    }
    public async Task<IReadOnlyList<MonthlyBalancePoint>> GetMonthlyEndBalancesAsync(
        long accountId,
        DateOnly startMonthInclusive,   // must be the 1st of a month
        DateOnly endMonthExclusive,     // must be the 1st of a month (one past last)
        CancellationToken ct)
    {
        const string sql = """
    WITH RECURSIVE months(m) AS (
        SELECT date(@startMonth)
        UNION ALL
        SELECT date(m, '+1 month')
        FROM months
        WHERE m < date(@endMonthExclusive, '-1 month')
    ),
    acct AS (
        SELECT opening_balance_cents AS opening
        FROM accounts
        WHERE id = @accountId
    ),
    monthly AS (
        SELECT
            m AS month_start,
            date(m, '+1 month', '-1 day') AS month_end
        FROM months
    )
    SELECT
        monthly.month_start AS MonthStart,
        monthly.month_end AS MonthEnd,
        (
            (SELECT opening FROM acct)
            +
            COALESCE((
            SELECT SUM(t.amount_cents)
            FROM transactions t
            WHERE t.account_id = @accountId
                AND date(t.txn_date) <= date(monthly.month_end)
            ), 0)
        ) AS EndBalanceCents
    FROM monthly
    ORDER BY monthly.month_start;
    """;

        using var conn = _factory.CreateOpenConnection();

        // SQLite returns MonthStart/MonthEnd as text; we map manually.
        var rows = await conn.QueryAsync(
            new CommandDefinition(
                sql,
                new
                {
                    accountId,
                    startMonth = startMonthInclusive.ToString("yyyy-MM-dd"),
                    endMonthExclusive = endMonthExclusive.ToString("yyyy-MM-dd")
                },
                cancellationToken: ct));

        var list = new List<MonthlyBalancePoint>();

        foreach (var r in rows)
        {
            // Dapper returns dynamic; pull out strings
            string ms = r.MonthStart;
            string me = r.MonthEnd;
            long cents = (long)r.EndBalanceCents;

            list.Add(new MonthlyBalancePoint(
                DateOnly.Parse(ms),
                DateOnly.Parse(me),
                cents));
        }

        return list;
    }
}
