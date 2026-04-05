using System.Data.Common;
using Npgsql;

namespace ChatbotMVC.Data.Providers;

/// <summary>
/// Proveedor para PostgreSQL.
/// Requiere el paquete NuGet: Npgsql (se activa cuando Provider = "PostgreSQL")
/// </summary>
public class PostgreSqlProvider : BaseDbProvider
{
    public PostgreSqlProvider(string connectionString, ILogger logger)
        : base(connectionString, logger) { }

    public override string ProviderName => "PostgreSQL";

    public override async Task<DbConnection> OpenConnectionAsync()
    {
        var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        return conn;
    }

    public override string GetSqlDialectHints() => """
        Motor: PostgreSQL
        - Usa LIMIT en lugar de TOP:       SELECT * FROM tabla LIMIT 10
        - Fecha actual:                    NOW()  o  CURRENT_DATE
        - Partes de fecha:                 EXTRACT(YEAR FROM fecha), EXTRACT(MONTH FROM fecha)
        - Formatear fecha:                 TO_CHAR(fecha, 'YYYY-MM')
        - Concatenar strings:              'texto' || columna  o  CONCAT(a, b)
        - Conversión de tipo:              CAST(x AS INTEGER)  o  x::INTEGER
        - Verificar NULL:                  COALESCE(col, valor_defecto)
        - Booleano:                        activo = TRUE  o  activo = FALSE
        - Paginación:                      LIMIT 5 OFFSET 10
        - Strings sensibles a mayúsculas:  usar ILIKE para búsqueda insensible
        - Auto incremento:                 SERIAL  o  GENERATED ALWAYS AS IDENTITY
        """;

    public override string GetCreateTablesSql() => """
        CREATE TABLE IF NOT EXISTS departamentos (
            id          SERIAL PRIMARY KEY,
            nombre      VARCHAR(100) NOT NULL,
            presupuesto NUMERIC(18,2) NOT NULL
        );

        CREATE TABLE IF NOT EXISTS empleados (
            id              SERIAL PRIMARY KEY,
            nombre          VARCHAR(150) NOT NULL,
            cargo           VARCHAR(100) NOT NULL,
            salario         NUMERIC(18,2) NOT NULL,
            departamento_id INTEGER NOT NULL,
            fecha_contrato  DATE    NOT NULL,
            activo          BOOLEAN NOT NULL DEFAULT TRUE,
            FOREIGN KEY (departamento_id) REFERENCES departamentos(id)
        );

        CREATE TABLE IF NOT EXISTS productos (
            id        SERIAL PRIMARY KEY,
            nombre    VARCHAR(150) NOT NULL,
            categoria VARCHAR(100) NOT NULL,
            precio    NUMERIC(18,2) NOT NULL,
            stock     INTEGER NOT NULL
        );

        CREATE TABLE IF NOT EXISTS clientes (
            id     SERIAL PRIMARY KEY,
            nombre VARCHAR(150) NOT NULL,
            ciudad VARCHAR(100) NOT NULL,
            email  VARCHAR(200) NOT NULL
        );

        CREATE TABLE IF NOT EXISTS ventas (
            id          SERIAL PRIMARY KEY,
            empleado_id INTEGER     NOT NULL,
            producto_id INTEGER     NOT NULL,
            cantidad    INTEGER     NOT NULL,
            fecha       DATE        NOT NULL,
            total       NUMERIC(18,2) NOT NULL,
            FOREIGN KEY (empleado_id) REFERENCES empleados(id),
            FOREIGN KEY (producto_id) REFERENCES productos(id)
        );
        """;

    public override string GetTableExistsSql(string tableName) =>
        $"SELECT COUNT(*) FROM information_schema.tables WHERE table_name='{tableName}' AND table_schema='public'";
}
