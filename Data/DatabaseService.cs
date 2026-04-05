using Microsoft.Data.SqlClient;
using System.Text;

namespace ChatbotMVC.Data;

/// <summary>
/// Servicio de base de datos.
/// Lee el schema real de cualquier BD SQL Server conectada.
/// Solo cambia el connection string en appsettings.json → todo se adapta solo.
/// </summary>
public class DatabaseService
{
    private readonly string _conn;
    private readonly ILogger<DatabaseService> _logger;

    public string DatabaseName { get; private set; } = "";

    public DatabaseService(IConfiguration config, ILogger<DatabaseService> logger)
    {
        _conn   = config.GetConnectionString("DefaultConnection")
                  ?? throw new Exception("Falta 'DefaultConnection' en appsettings.json");
        _logger = logger;
    }

    // ──────────────────────────────────────────────────────────────
    // Al arrancar: solo leer el nombre de la BD conectada
    // ──────────────────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        try
        {
            await using var conn = new SqlConnection(_conn);
            await conn.OpenAsync();
            DatabaseName = conn.Database;
            _logger.LogInformation("Conectado a SQL Server. BD: {DB}", DatabaseName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al conectar con la base de datos.");
            throw;
        }
    }

    // ──────────────────────────────────────────────────────────────
    // Ejecutar cualquier consulta SELECT
    // ──────────────────────────────────────────────────────────────

    public async Task<QueryResult> ExecuteQueryAsync(string sql)
    {
        var result = new QueryResult();
        try
        {
            await using var conn = new SqlConnection(_conn);
            await conn.OpenAsync();
            await using var cmd    = new SqlCommand(sql, conn) { CommandTimeout = 30 };
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
            result.Success = false;
            result.Error   = ex.Message;
            _logger.LogError(ex, "Error ejecutando consulta.");
        }
        return result;
    }

    // ──────────────────────────────────────────────────────────────
    // Leer el schema REAL de la BD conectada (para la IA y la UI)
    // Funciona con EmpresaDB, AutosDB, VentasDB o cualquier otra.
    // ──────────────────────────────────────────────────────────────

    public async Task<List<TableInfo>> GetSchemaAsync()
    {
        // Consulta INFORMATION_SCHEMA para obtener todas las tablas y columnas
        const string sql = """
            SELECT
                t.TABLE_SCHEMA,
                t.TABLE_NAME,
                c.COLUMN_NAME,
                c.DATA_TYPE,
                c.IS_NULLABLE,
                c.COLUMN_DEFAULT,
                CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 'YES' ELSE 'NO' END AS IS_PRIMARY_KEY,
                fk.REFERENCED_TABLE AS FK_TABLE,
                fk.REFERENCED_COLUMN AS FK_COLUMN
            FROM INFORMATION_SCHEMA.TABLES t
            JOIN INFORMATION_SCHEMA.COLUMNS c
                ON t.TABLE_NAME = c.TABLE_NAME AND t.TABLE_SCHEMA = c.TABLE_SCHEMA
            -- Primary keys
            LEFT JOIN (
                SELECT ku.TABLE_NAME, ku.COLUMN_NAME
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
                    ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
            ) pk ON pk.TABLE_NAME = c.TABLE_NAME AND pk.COLUMN_NAME = c.COLUMN_NAME
            -- Foreign keys
            LEFT JOIN (
                SELECT
                    ku.TABLE_NAME, ku.COLUMN_NAME,
                    ku2.TABLE_NAME  AS REFERENCED_TABLE,
                    ku2.COLUMN_NAME AS REFERENCED_COLUMN
                FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
                JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
                    ON rc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku2
                    ON rc.UNIQUE_CONSTRAINT_NAME = ku2.CONSTRAINT_NAME
            ) fk ON fk.TABLE_NAME = c.TABLE_NAME AND fk.COLUMN_NAME = c.COLUMN_NAME
            WHERE t.TABLE_TYPE = 'BASE TABLE'
            ORDER BY t.TABLE_NAME, c.ORDINAL_POSITION
            """;

        var result = await ExecuteQueryAsync(sql);
        if (!result.Success) return new List<TableInfo>();

        // Agrupar por tabla
        var tables = new Dictionary<string, TableInfo>();
        foreach (var row in result.Rows)
        {
            var schemaName = row["TABLE_SCHEMA"]?.ToString() ?? "";
            var tableName = row["TABLE_NAME"]?.ToString() ?? "";
            var key = $"{schemaName}.{tableName}";

            if (!tables.ContainsKey(key))
                tables[key] = new TableInfo { Schema = schemaName, Name = tableName };

            tables[key].Columns.Add(new ColumnInfo
            {
                Name = row["COLUMN_NAME"]?.ToString() ?? "",
                DataType = row["DATA_TYPE"]?.ToString() ?? "",
                Nullable = row["IS_NULLABLE"]?.ToString() == "YES",
                IsPK = row["IS_PRIMARY_KEY"]?.ToString() == "YES",
                FkTable = row["FK_TABLE"]?.ToString(),
                FkColumn = row["FK_COLUMN"]?.ToString()
            });

        }

        // Contar filas de cada tabla
        foreach (var table in tables.Values)
        {
            var countResult = await ExecuteQueryAsync(
                $"SELECT COUNT(*) AS n FROM [{table.Schema}].[{table.Name}]"
            );
            if (countResult.Success && countResult.Rows.Count > 0)
                table.RowCount = Convert.ToInt32(countResult.Rows[0]["n"] ?? 0);
        }

        return tables.Values.ToList();

    }

    // ──────────────────────────────────────────────────────────────
    // Construir el texto de schema para el prompt de Gemini
    // Se genera automáticamente desde el schema real de la BD
    // ──────────────────────────────────────────────────────────────

    public async Task<string> BuildSchemaPromptAsync()
    {
        var tables = await GetSchemaAsync();
        var sb     = new StringBuilder();

        sb.AppendLine($"Base de datos SQL Server: [{DatabaseName}]");
        sb.AppendLine();
        sb.AppendLine("TABLAS DISPONIBLES:");

        foreach (var table in tables)
        {
            sb.Append($"  {table.Schema}.{table.Name} (");

            var colDefs = table.Columns.Select(c =>
            {
                var def = c.Name;
                if (c.IsPK)  def += " PK";
                if (c.FkTable != null) def += $" FK→{c.FkTable}.{c.FkColumn}";
                def += $" [{c.DataType}]";
                if (!c.Nullable && !c.IsPK) def += " NOT NULL";
                return def;
            });
            sb.AppendLine(string.Join(", ", colDefs) + ")");
        }

        sb.AppendLine();
        sb.AppendLine("REGLAS T-SQL (SQL Server):");
        sb.AppendLine("  - Usa TOP en lugar de LIMIT:  SELECT TOP 10 * FROM tabla");
        sb.AppendLine("  - Fecha actual: GETDATE()");
        sb.AppendLine("  - Partes de fecha: YEAR(col), MONTH(col), DAY(col)");
        sb.AppendLine("  - Booleanos como BIT: col = 1 o col = 0");
        sb.AppendLine("  - Nombres con espacios: usar corchetes [nombre tabla]");
        sb.AppendLine("  - Las consultas debe usar esquemas: SELECT * FROM [schema].[tabla]");

        return sb.ToString();
    }
}

// ── DTOs ──────────────────────────────────────────────────────────

public class QueryResult
{
    public bool    Success { get; set; }
    public string? Error   { get; set; }
    public List<string>                      Columns { get; set; } = new();
    public List<Dictionary<string, object?>> Rows    { get; set; } = new();
    public int RowCount => Rows.Count;
}

public class TableInfo
{
    public string           Schema   { get; set; } = "";
    public string           Name     { get; set; } = "";
    public int              RowCount { get; set; }
    public List<ColumnInfo> Columns  { get; set; } = new();
}

public class ColumnInfo
{
    public string  Name     { get; set; } = "";
    public string  DataType { get; set; } = "";
    public bool    Nullable { get; set; }
    public bool    IsPK     { get; set; }
    public string? FkTable  { get; set; }
    public string? FkColumn { get; set; }
}
