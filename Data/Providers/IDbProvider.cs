using System.Data;
using System.Data.Common;

namespace ChatbotMVC.Data.Providers;

/// <summary>
/// Contrato que debe implementar cada proveedor de base de datos.
/// Agregar un nuevo motor (MySQL, PostgreSQL, SQLite…) = crear una nueva clase que implemente esta interfaz.
/// </summary>
public interface IDbProvider
{
    /// <summary>Nombre del proveedor para logs y UI (ej: "SQL Server", "PostgreSQL").</summary>
    string ProviderName { get; }

    /// <summary>Crea y retorna una conexión abierta.</summary>
    Task<DbConnection> OpenConnectionAsync();

    /// <summary>Ejecuta uno o más comandos DDL/DML sin retornar filas.</summary>
    Task ExecuteNonQueryAsync(DbConnection conn, string sql);

    /// <summary>Ejecuta un SELECT y retorna filas como lista de diccionarios.</summary>
    Task<QueryResult> ExecuteQueryAsync(string sql);

    /// <summary>
    /// Ejecuta un escalar (COUNT, SUM…) y retorna el valor.
    /// </summary>
    Task<object?> ExecuteScalarAsync(DbConnection conn, string sql);

    /// <summary>
    /// Devuelve el SQL dialect hint para la IA (diferencias entre motores).
    /// </summary>
    string GetSqlDialectHints();

    /// <summary>
    /// Devuelve DDL compatible con el motor para crear las tablas de la app.
    /// </summary>
    //string GetCreateTablesSql();

    /// <summary>
    /// Devuelve la consulta para verificar si una tabla existe.
    /// </summary>
    string GetTableExistsSql(string tableName);
}
