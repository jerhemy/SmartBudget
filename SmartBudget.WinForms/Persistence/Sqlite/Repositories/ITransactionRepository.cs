using SmartBudget.Domain;

namespace SmartBudget.WinForms.Persistence.Sqlite.Repositories
{
    public sealed record TransactionRow(long Id, DateOnly Date, string Description, string Memo, long AmountCents);

    public interface ITransactionRepository
    {
        Task<bool> AddTransaction(long accountId, DateOnly date, string title, double amount, CancellationToken ct);

        Task<bool> DeleteTransactionById(long transactionId, CancellationToken ct);

        Task<bool> UpsertTransaction(long? transactionId, long accountId, DateOnly date, string title, string description, double amount, CancellationToken ct);

        Task<long> GetSumBeforeDateAsync(long accountId, DateOnly beforeDateExclusive, CancellationToken ct);
        Task<long> GetTotal(DateOnly beforeDateExclusive, CancellationToken ct);

        Task<IReadOnlyList<TransactionRow>> GetForRangeAsync(long accountId, DateOnly startInclusive, DateOnly endExclusive, CancellationToken ct);

        Task UpdateDateAsync(long transactionId, DateOnly newDate, CancellationToken ct);

        Task<InsertImportedResult> InsertImportedAsync(long accountId, string source, IReadOnlyList<ImportedTransaction> parsed, CancellationToken ct);

        Task InsertImportedAsync(InsertImportedTransaction transaction, CancellationToken ct);

        Task<int> BulkInsertAsync(IReadOnlyList<InsertTransactionRequest> items, CancellationToken ct);
    }
}
