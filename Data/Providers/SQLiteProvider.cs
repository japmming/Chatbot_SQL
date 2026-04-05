using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace ChatbotMVC.Data.Providers;

/// <summary>
/// Proveedor para SQLite (ideal para desarrollo local y pruebas).
/// No requiere servidor — la BD es un archivo .db local.
/// </summary>
public class SQLiteProvider : BaseDbProvider
{
    public SQLiteProvider(string connectionString, ILogger logger)
        : base(connectionString, logger) { }

    public override string ProviderName => "SQLite";

    public override async Task<DbConnection> OpenConnectionAsync()
    {
        var conn = new SqliteConnection(ConnectionString);
        await conn.OpenAsync();
        return conn;
    }

    public override string GetSqlDialectHints() => """
        Motor: SQLite
        - Usa LIMIT en lugar de TOP:       SELECT * FROM tabla LIMIT 10
        - Fecha actual:                    date('now')  o  datetime('now')
        - Partes de fecha:                 strftime('%Y', fecha), strftime('%m', fecha)
        - Formatear fecha:                 strftime('%Y-%m', fecha)
        - Concatenar strings:              'texto' || columna  o  CONCAT(a, b)
        - Conversión de tipo:              CAST(x AS INTEGER)
        - Verificar NULL:                  IFNULL(col, valor_defecto)  o  COALESCE
        - Booleano:                        activo = 1  o  activo = 0  (INTEGER)
        - Paginación:                      LIMIT 5 OFFSET 10
        - Sin soporte para RIGHT JOIN:     usar LEFT JOIN con tablas invertidas
        """;

    public override string GetCreateTablesSql() => """
        CREATE TABLE IF NOT EXISTS departamentos (
            id          INTEGER PRIMARY KEY AUTOINCREMENT,
            nombre      TEXT    NOT NULL,
            presupuesto REAL    NOT NULL
        );

        CREATE TABLE IF NOT EXISTS empleados (
            id              INTEGER PRIMARY KEY AUTOINCREMENT,
            nombre          TEXT    NOT NULL,
            cargo           TEXT    NOT NULL,
            salario         REAL    NOT NULL,
            departamento_id INTEGER NOT NULL,
            fecha_contrato  TEXT    NOT NULL,
            activo          INTEGER NOT NULL DEFAULT 1,
            FOREIGN KEY (departamento_id) REFERENCES departamentos(id)
        );

        CREATE TABLE IF NOT EXISTS productos (
            id        INTEGER PRIMARY KEY AUTOINCREMENT,
            nombre    TEXT NOT NULL,
            categoria TEXT NOT NULL,
            precio    REAL NOT NULL,
            stock     INTEGER NOT NULL
        );

        CREATE TABLE IF NOT EXISTS clientes (
            id     INTEGER PRIMARY KEY AUTOINCREMENT,
            nombre TEXT NOT NULL,
            ciudad TEXT NOT NULL,
            email  TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS ventas (
            id          INTEGER PRIMARY KEY AUTOINCREMENT,
            empleado_id INTEGER NOT NULL,
            producto_id INTEGER NOT NULL,
            cantidad    INTEGER NOT NULL,
            fecha       TEXT    NOT NULL,
            total       REAL    NOT NULL,
            FOREIGN KEY (empleado_id) REFERENCES empleados(id),
            FOREIGN KEY (producto_id) REFERENCES productos(id)
        );
        """;

    public override string GetTableExistsSql(string tableName) =>
        $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{tableName}'";
}
