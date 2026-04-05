using System.Data.Common;

namespace ChatbotMVC.Data.Providers;

/// <summary>
/// Clase base con la lógica común de ejecución de consultas.
/// Cada proveedor concreto solo necesita implementar la creación de conexión y el DDL específico.
/// </summary>
public abstract class BaseDbProvider : IDbProvider
{
    protected readonly string ConnectionString;
    protected readonly ILogger Logger;

    protected BaseDbProvider(string connectionString, ILogger logger)
    {
        ConnectionString = connectionString;
        Logger           = logger;
    }

    public abstract string ProviderName { get; }
    public abstract Task<DbConnection> OpenConnectionAsync();
    public abstract string GetSqlDialectHints();
    //public abstract string GetCreateTablesSql();
    public abstract string GetTableExistsSql(string tableName);

    // ─────────────────────────────────────────────────────────────────────────
    // Implementaciones compartidas
    // ─────────────────────────────────────────────────────────────────────────

    public async Task ExecuteNonQueryAsync(DbConnection conn, string sql)
    {
        // Algunos motores no permiten múltiples statements en un solo comando.
        // Dividimos por ";" y ejecutamos uno a uno, ignorando líneas vacías.
        var statements = sql
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0);

        foreach (var stmt in statements)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText    = stmt;
            cmd.CommandTimeout = 60;
            await cmd.ExecuteNonQueryAsync();
        }
    }

    public async Task<QueryResult> ExecuteQueryAsync(string sql)
    {
        var result = new QueryResult();
        try
        {
            await using var conn = await OpenConnectionAsync();
            await using var cmd  = conn.CreateCommand();
            cmd.CommandText    = sql;
            cmd.CommandTimeout = 30;

            await using var reader = await cmd.ExecuteReaderAsync();

            for (int i = 0; i < reader.FieldCount; i++)
                result.Columns.Add(reader.GetName(i));

            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                result.Rows.Add(row);
            }

            result.Success = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error ejecutando consulta: {Sql}", sql);
            result.Success = false;
            result.Error   = ex.Message;
        }
        return result;
    }

    public async Task<object?> ExecuteScalarAsync(DbConnection conn, string sql)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText    = sql;
        cmd.CommandTimeout = 30;
        return await cmd.ExecuteScalarAsync();
    }
}
