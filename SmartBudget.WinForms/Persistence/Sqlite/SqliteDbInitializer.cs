using Microsoft.Data.Sqlite;
using SmartBudget.WinForms;
using System.Data;
using System.Reflection;

namespace SmartBudget.Infrastructure.Persistence.Sqlite;

public interface IDbInitializer
{
    void Initialize();
}

public sealed class SqliteDbInitializer : IDbInitializer
{
    private readonly DbOptions _opts;
    private readonly SqliteConnectionFactory _factory;

    public SqliteDbInitializer(DbOptions opts, SqliteConnectionFactory factory)
    {
        _factory = factory;
        _opts = opts;
    }

    public void Initialize()
    {

        try
        {

            EnsureDirectoryExists(_opts.DbFilePath);

            using var conn = _factory.CreateOpenConnection();
            using var tx = conn.BeginTransaction();

            var schemaSql = LoadEmbeddedResourceText(
                assembly: typeof(SqliteDbInitializer).Assembly,
                resourceEndsWith: ".Persistence.Sqlite.Scripts.schema.sql");

            if (string.IsNullOrWhiteSpace(schemaSql))
                throw new InvalidOperationException("Embedded schema.sql was empty or not found.");

            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = schemaSql;
            cmd.ExecuteNonQuery();

            tx.Commit();
        }
        catch (SqliteException ex)
        {
            Console.WriteLine($"SQLite error during database initialization: {ex.Message}");
        }
    }

    private static void EnsureDirectoryExists(string dbFilePath)
    {
        var dir = Path.GetDirectoryName(dbFilePath);
        if (string.IsNullOrWhiteSpace(dir))
            return;

        Directory.CreateDirectory(dir);
    }

    private static string LoadEmbeddedResourceText(Assembly assembly, string resourceEndsWith)
    {
        var name = assembly
            .GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(resourceEndsWith, StringComparison.OrdinalIgnoreCase));

        if (name is null) return string.Empty;

        using var stream = assembly.GetManifestResourceStream(name);
        if (stream is null) return string.Empty;

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
