namespace SmartBudget;

public sealed record ImportedTransaction(
    DateOnly PostedDate,
    decimal Amount,
    string Description,
    bool IsCleared,
    string? CheckNumber,
    int SourceLineNumber,
    string ImportHash);


public sealed record CreateAccount(string Name, string ExternalKey);

public sealed record InsertImportedTransaction(
    long AccountId,
    DateOnly PostedDate,
    decimal Amount,
    string Title,
    string? Memo,
    string Source,
    string? ExternalId,
    string? ExternalHash);