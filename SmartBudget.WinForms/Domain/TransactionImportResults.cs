namespace SmartBudget
{
    public sealed record TransactionImportResult(
        int Parsed,
        int Inserted,
        int SkippedAsDuplicate);
}
