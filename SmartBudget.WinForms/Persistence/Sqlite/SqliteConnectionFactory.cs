using System.Data;
using Microsoft.Data.Sqlite;
using SmartBudget.WinForms;

namespace SmartBudget.Infrastructure.Persistence.Sqlite;

public interface IAppDbConnectionFactory
{
    IDbConnection CreateOpenConnection();
}

public sealed class SqliteConnectionFactory : IAppDbConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(DbOptions opts)
    {
        _connectionString = opts.ConnectionString;
    }

    public IDbConnection CreateOpenConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();

        // Make sure FK constraints are actually enforced:
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA foreign_keys = ON; PRAGMA journal_mode = WAL; PRAGMA synchronous = NORMAL;";
        cmd.ExecuteNonQuery();

        return conn;
    }
}
