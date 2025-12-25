namespace SmartBudget.WinForms.Quicken;

public sealed record ImportedStatement(
    string ExternalAccountKey,
    string BankId,
    string AccountId,
    string AccountType,
    IReadOnlyList<QuickenImportedTransaction> Transactions);