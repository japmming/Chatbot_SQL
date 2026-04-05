using ChatbotMVC.Data.Providers;

namespace ChatbotMVC.Data.Schema;

/// <summary>
/// Lee el esquema real de la base de datos independientemente del motor.
/// Genera tanto la descripción para la IA como la lista visual para la UI.
/// </summary>
public class SchemaInspector
{
    private readonly IDbProvider _provider;

    private static readonly string[] AppTables ={};

    public SchemaInspector(IDbProvider provider)
    {
        _provider = provider;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Descripción textual para el prompt de la IA
    // ─────────────────────────────────────────────────────────────────────────

    //public string BuildSchemaDescription()
    //{
    //    var sb = new System.Text.StringBuilder();
    //    sb.AppendLine($"Base de datos ({_provider.ProviderName}): Sistema de gestión empresarial");
    //    sb.AppendLine();
    //    sb.AppendLine("TABLAS DISPONIBLES:");
    //    sb.AppendLine("  departamentos  : id(PK), nombre, presupuesto");
    //    sb.AppendLine("  empleados      : id(PK), nombre, cargo, salario, departamento_id(FK→departamentos.id), fecha_contrato, activo");
    //    sb.AppendLine("  productos      : id(PK), nombre, categoria, precio, stock");
    //    sb.AppendLine("  ventas         : id(PK), empleado_id(FK→empleados.id), producto_id(FK→productos.id), cantidad, fecha, total");
    //    sb.AppendLine("  clientes       : id(PK), nombre, ciudad, email");
    //    sb.AppendLine();
    //    sb.AppendLine("HINTS DEL DIALECTO SQL:");
    //    sb.AppendLine(_provider.GetSqlDialectHints());
    //    return sb.ToString();
    //}

    // ─────────────────────────────────────────────────────────────────────────
    // Metadatos para la UI (sidebar con tablas y columnas)
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<List<SchemaTable>> GetSchemaTablesAsync()
    {
        var tables = new List<SchemaTable>();

        foreach (var tableName in AppTables)
        {
            try
            {
                var table = new SchemaTable { Name = tableName };

                // Contar filas
                var countResult = await _provider.ExecuteQueryAsync(
                    $"SELECT COUNT(*) AS total FROM {tableName}");
                if (countResult.Success && countResult.Rows.Count > 0)
                    table.RowCount = Convert.ToInt32(countResult.Rows[0].Values.First() ?? 0);

                // Obtener columnas con una consulta de 0 filas (compatible con todos los motores)
                var colResult = await _provider.ExecuteQueryAsync(
                    $"SELECT * FROM {tableName} WHERE 1=0");
                if (colResult.Success)
                {
                    table.Columns = colResult.Columns.Select(c => new SchemaColumn
                    {
                        Name     = c,
                        DataType = "",
                        Nullable = c != "id"
                    }).ToList();
                }

                tables.Add(table);
            }
            catch
            {
                // Si la tabla no existe aún, omitirla silenciosamente
            }
        }

        return tables;
    }
}
