using Dapper;
using SmartBudget.Domain;
using SmartBudget.Infrastructure.Persistence.Sqlite;
using System.Net.NetworkInformation;
using System.Security.Principal;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SmartBudget.WinForms.Persistence.Sqlite.Repositories
{
    public sealed class SqliteAccountRepository : IAccountRepository
    {
        private readonly SqliteConnectionFactory _factory;

        public SqliteAccountRepository(SqliteConnectionFactory factory) => _factory = factory;

        public async Task<IReadOnlyList<AccountListItem>> GetAllAsync(CancellationToken ct)
        {
            const string sql = """
              
                SELECT id AS Id,       
                name AS Name,       
                opening_balance_cents AS OpeningBalanceCents
                FROM accounts 
                ORDER BY name;
         """;
                

            using var conn = _factory.CreateOpenConnection();
            var rows = await conn.QueryAsync<AccountListItem>(new CommandDefinition(sql, cancellationToken: ct));
            return rows.AsList();
        }

        public async Task<AccountInfo?> GetByIdAsync(long accountId, CancellationToken ct)
        {
            const string sql = """
                SELECT id AS Id,
                       name AS Name,
                       opening_balance_cents AS OpeningBalanceCents
                FROM accounts
                WHERE id = @accountId
                LIMIT 1;
                """;

            using var conn = _factory.CreateOpenConnection();
            return await conn.QuerySingleOrDefaultAsync<AccountInfo>(new CommandDefinition(sql, new { accountId }, cancellationToken: ct));
        }

        public async Task<AccountInfo?> GetByExternalKeyAsync(string externalKey, CancellationToken ct)
        {
            const string sql = """
                SELECT 
                    id AS Id,
                    name AS Name,
                    opening_balance_cents AS OpeningBalanceCents
                FROM accounts
                WHERE external_key = @externalKey
                LIMIT 1;
                """;
            using var conn = _factory.CreateOpenConnection();
            return await conn.QuerySingleOrDefaultAsync<AccountInfo>(new CommandDefinition(sql, new { externalKey }, cancellationToken: ct));
        }

        //public async Task<AccountListItem> GetAccountById(long accountId, CancellationToken ct)
        //{
        //    try
        //    {
        //        const string sql = """
        //        SELECT 
        //            id AS Id,
        //            name AS Name,
        //            opening_balance_cents AS OpeningBalanceCents
        //        FROM accounts
        //        WHERE id = @accountId
        //        LIMIT 1;
        //        """;

        //        using var conn = _factory.CreateOpenConnection();

        //        var account = await conn.QuerySingleOrDefaultAsync<AccountListItem>(new CommandDefinition(sql, new { accountId }, cancellationToken: ct));

        //        return account;

        //    } catch (Exception ex)
        //    {
                
        //    }

        //    return default;

        //}

        public async Task<bool> AddAccount(long accountId, string accountName, CancellationToken ct)
        {
            const string sql = """
                INSERT INTO accounts (id, name) VALUES (@accountId, @accountName);
                """;

            using var conn = _factory.CreateOpenConnection();
            var rows = await conn.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        accountId,
                        accountName = accountName,
                    },
                    cancellationToken: ct));

            return rows == 1;
        }
    }
}
