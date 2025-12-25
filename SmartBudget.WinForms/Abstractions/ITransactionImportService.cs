namespace SmartBudget.WinForms.Abstractions
{
    public interface ITransactionImportService
    {
        Task<TransactionImportResult> ImportAsync(
            long accountId,
            string csvText,
            string sourceName,
            CancellationToken ct);
    }
}
