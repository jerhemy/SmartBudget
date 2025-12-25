using Dapper;
using SmartBudget.Domain;
using SmartBudget.Infrastructure.Persistence.Sqlite;

namespace SmartBudget.WinForms.Persistence.Sqlite.Repositories;

public sealed class SqliteTransactionRepository : ITransactionRepository
{
    private readonly SqliteConnectionFactory _factory;

    public SqliteTransactionRepository(SqliteConnectionFactory factory) => _factory = factory;

    public async Task<bool> AddTransaction(long accountId, DateOnly date, string title, double amount, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO transactions
            (account_id, txn_date, description, amount_cents)
            VALUES
            (@accountId, @date, @description, @amountCents);
            """;

        var amountCents = amount * 100;
        using var conn = _factory.CreateOpenConnection();
        var rows = await conn.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    accountId,
                    date = date.ToString("yyyy-MM-dd"),
                    description = title,
                    amountCents
                },
                cancellationToken: ct));
        return rows == 1;
    }

    public async Task<bool> DeleteTransactionById(long transactionId, CancellationToken ct)
    {
        const string sql = """
            DELETE FROM transactions
            WHERE id = @transactionId;
            """;

        using var conn = _factory.CreateOpenConnection();
        var rows = await conn.ExecuteAsync(new CommandDefinition(sql, new { transactionId }, cancellationToken: ct));
        return rows == 1;
    }


    public async Task<bool> UpsertTransaction(long? transactionId, long accountId, DateOnly date, string title, string description, double amount, CancellationToken ct)
    {
        if (transactionId.HasValue)
        {
            const string sqlUpdate = """
                UPDATE transactions
                SET txn_date = @date,
                    memo = @title,
                    description = @description,
                    amount_cents = @amountCents
                WHERE id = @id;
                """;
            var amountCents = amount * 100;
            using var conn = _factory.CreateOpenConnection();
            var rows = await conn.ExecuteAsync(
                new CommandDefinition(
                    sqlUpdate,
                    new
                    {
                        id = transactionId.Value,
                        date = date.ToString("yyyy-MM-dd"),
                        title,
                        description,
                        amountCents
                    },
                    cancellationToken: ct));
            return rows == 1;
        }
        else
        {
            return await AddTransaction(accountId, date, title, amount, ct);
        }
    }

    public async Task<long> GetSumBeforeDateAsync(long accountId, DateOnly beforeDateExclusive, CancellationToken ct)
        {
            const string sql = """
            SELECT COALESCE(SUM(amount_cents), 0)
            FROM transactions
            WHERE account_id = @accountId
              AND txn_date < @before;
            """;

            using var conn = _factory.CreateOpenConnection();
            return await conn.ExecuteScalarAsync<long>(
                new CommandDefinition(sql, new
                {
                    accountId,
                    before = beforeDateExclusive.ToString("yyyy-MM-dd")
                }, cancellationToken: ct));
        }

    public async Task<long> GetTotal(DateOnly beforeDateExclusive, CancellationToken ct)
    {
        const string sql = """
            SELECT COALESCE(SUM(amount_cents), 0)
            FROM transactions
            WHERE txn_date < @before;
            """;

        using var conn = _factory.CreateOpenConnection();
        return await conn.ExecuteScalarAsync<long>(
            new CommandDefinition(sql, new
            {
                before = beforeDateExclusive.ToString("yyyy-MM-dd")
            }, cancellationToken: ct));
    }

    public async Task UpdateDateAsync(long transactionId, DateOnly newDate, CancellationToken ct)
    {
        const string sql = """
            UPDATE transactions
            SET txn_date = @date
            WHERE id = @id;
            """;

        using var conn = _factory.CreateOpenConnection();

        var rows = await conn.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    id = transactionId,
                    date = newDate.ToString("yyyy-MM-dd")
                },
                cancellationToken: ct));

        if (rows != 1)
            throw new InvalidOperationException($"Expected 1 row updated, got {rows} (transactionId={transactionId}).");
    }


    public async Task<IReadOnlyList<TransactionRow>> GetForRangeAsync(
           long accountId,
           DateOnly startInclusive,
           DateOnly endExclusive,
           CancellationToken ct)
    {
        const string sql = """
        SELECT
            id,
            txn_date,
            description,
            memo,
            amount_cents
        FROM transactions
        WHERE account_id = @accountId
          AND txn_date >= @start
          AND txn_date <  @end
        ORDER BY txn_date ASC, id ASC;
        """;

        using var conn = _factory.CreateOpenConnection();

        var rows = await conn.QueryAsync<dynamic>(
            new CommandDefinition(
                sql,
                new
                {
                    accountId,
                    start = startInclusive.ToString("yyyy-MM-dd"),
                    end = endExclusive.ToString("yyyy-MM-dd")
                },
                cancellationToken: ct));

        var list = new List<TransactionRow>();

        foreach (var r in rows)
        {
            list.Add(new TransactionRow(
                Id: (long)r.id,
                Date: DateOnly.Parse((string)r.txn_date),
                Memo: (string)r.memo,
                Description: r.description,
                AmountCents: (long)r.amount_cents
            ));
        }

        return list;
    }


    public async Task<InsertImportedResult> InsertImportedAsync(
          long accountId,
          string source,
          IReadOnlyList<ImportedTransaction> parsed,
          CancellationToken ct)
    {
        if (parsed.Count == 0)
            return new InsertImportedResult(0, 0);

        // IMPORTANT: adjust column names to match your schema if different.
        // This assumes:
        // - txn_date TEXT (yyyy-MM-dd)
        // - amount_cents INTEGER
        // - description TEXT
        // - is_cleared INTEGER (0/1) [optional - remove if not in schema]
        // - source TEXT
        // - import_hash TEXT
        const string sql = """
        INSERT INTO transactions
        (
            account_id,
            txn_date,
            title,
            description,
            amount_cents,
            is_cleared,
            source,
            import_hash
        )
        VALUES
        (
            @AccountId,
            @TxnDate,
            @Title,
            @Description,
            @AmountCents,
            @IsCleared,
            @Source,
            @ImportHash
        );
        """;

        using var conn = _factory.CreateOpenConnection();
        using var tx = conn.BeginTransaction();

        var inserted = 0;
        var skipped = 0;

        foreach (var t in parsed)
        {
            ct.ThrowIfCancellationRequested();

            var title = string.IsNullOrWhiteSpace(t.Description.Trim()) ? "No Description" : t.Description.Trim();
            var rows = await conn.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        AccountId = accountId,
                        TxnDate = t.PostedDate.ToString("yyyy-MM-dd"),
                        Title = title,
                        Description = (string?)null, // or: t.Description
                        AmountCents = ToCents(t.Amount),
                        IsCleared = t.IsCleared ? 1 : 0,
                        Source = source,
                        ImportHash = t.ImportHash
                    },
                    transaction: tx,
                    cancellationToken: ct));

            if (rows == 1) inserted++;
            else skipped++;
        }

        tx.Commit();

        return new InsertImportedResult(inserted, skipped);
    }

    private static long ToCents(decimal amount) => (long)decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);


    public async Task InsertImportedAsync(ImportedTransaction txn, CancellationToken ct)
    {
        //if (txn is null) throw new ArgumentNullException(nameof(txn));
        //if (txn.AccountId <= 0) throw new ArgumentOutOfRangeException(nameof(txn.AccountId));
        //if (string.IsNullOrWhiteSpace(txn.Title)) throw new ArgumentException("Title is required.", nameof(txn));
        //if (string.IsNullOrWhiteSpace(txn.Source)) throw new ArgumentException("Source is required.", nameof(txn));

        //// For imported transactions, you want at least ONE stable id.
        //if (string.IsNullOrWhiteSpace(txn.ExternalId) && string.IsNullOrWhiteSpace(txn.ImportHash))
        //    throw new ArgumentException("Imported transactions must have ExternalId (FITID) or ImportHash for de-dupe.", nameof(txn));

        //const string sql = """
        //    INSERT OR IGNORE INTO transactions
        //    (
        //        account_id,
        //        txn_date,
        //        title,
        //        description,
        //        category,
        //        amount_cents,
        //        is_cleared,
        //        source,
        //        external_id,
        //        import_hash,
        //        check_number
        //    )
        //    VALUES
        //    (
        //        @AccountId,
        //        @TxnDate,
        //        @Title,
        //        @Description,
        //        @Category,
        //        @AmountCents,
        //        @IsCleared,
        //        @Source,
        //        @ExternalId,
        //        @ImportHash,
        //        @CheckNumber
        //    );

        //    SELECT changes();
        //    """;

        //var args = new
        //{
        //    txn.AccountId,
        //    TxnDate = txn.TxnDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        //    txn.Title,
        //    txn.Description,
        //    txn.Category,
        //    txn.AmountCents,
        //    IsCleared = txn.IsCleared ? 1 : 0,
        //    Source = txn.Source.Trim(),
        //    ExternalId = string.IsNullOrWhiteSpace(txn.ExternalId) ? null : txn.ExternalId.Trim(),
        //    ImportHash = string.IsNullOrWhiteSpace(txn.ImportHash) ? null : txn.ImportHash.Trim(),
        //    CheckNumber = string.IsNullOrWhiteSpace(txn.CheckNumber) ? null : txn.CheckNumber.Trim(),
        //};


        //using var conn = _factory.CreateOpenConnection();
        //var rows = await conn.QuerySingleAsync(
        //    new CommandDefinition(
        //        sql,
        //        args,
        //        cancellationToken: ct));

        //return rows == 1;
        throw new NotImplementedException();
    }

    public Task InsertImportedAsync(InsertImportedTransaction transaction, CancellationToken ct)
    {
        throw new NotImplementedException();
    }


    public async Task<int> BulkInsertAsync(IReadOnlyList<InsertTransactionRequest> items, CancellationToken ct)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));
        if (items.Count == 0) return 0;

        const string sql = """
            INSERT OR IGNORE INTO transactions
            (account_id, txn_date, description, memo, amount_cents, external_id, check_number, source)
            VALUES
            (@accountId, @date, @description, @memo, @amount_cents, @external_id, @check_number, @source);
        """;

        using var conn = _factory.CreateOpenConnection();
        using var tx = conn.BeginTransaction();

        var parameters = items.Select(x => new
        {
            accountId = x.AccounId,
            date = x.Date.ToString("yyyy-MM-dd"),
            description = x.Description,
            memo = x.Memo,
            amount_cents = x.Amount * 100,  // Convert to cents
            external_id = x.ExternalId,
            check_number = x.CheckNumber,
            source = x.Source
        });

        var rows = await conn.ExecuteAsync(
            new CommandDefinition(
                sql,
                parameters,
                transaction: tx,
                cancellationToken: ct));

        tx.Commit();
        return rows;
    }

    Task<InsertImportedResult> ITransactionRepository.InsertImportedAsync(long accountId, string source, IReadOnlyList<ImportedTransaction> parsed, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
