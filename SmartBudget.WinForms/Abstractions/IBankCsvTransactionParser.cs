namespace SmartBudget.WinForms.Abstractions
{
    public interface IBankCsvTransactionParser
    {
        IReadOnlyList<ImportedTransaction> Parse(string csvText);
    }
}
