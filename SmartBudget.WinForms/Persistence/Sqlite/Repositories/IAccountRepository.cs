namespace SmartBudget.WinForms.Persistence.Sqlite.Repositories;

public sealed record AccountListItem(long Id, string Name, long OpeningBalanceCents);

public interface IAccountRepository
{
    Task<IReadOnlyList<AccountListItem>> GetAllAsync(CancellationToken ct);

    Task<AccountInfo?> GetByIdAsync(long accountId, CancellationToken ct);

    Task<AccountInfo?> GetByExternalKeyAsync(string externalKey, CancellationToken ct);

    //Task<AccountListItem> GetAccountById(long accountId, CancellationToken ct);


    Task<bool> AddAccount(long accountId, string accountName, CancellationToken ct);
}