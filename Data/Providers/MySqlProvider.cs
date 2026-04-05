using System.Data.Common;
using MySqlConnector;

namespace ChatbotMVC.Data.Providers;

/// <summary>
/// Proveedor para MySQL / MariaDB.
/// Requiere el paquete NuGet: MySqlConnector (se activa cuando Provider = "MySQL")
/// </summary>
public class MySqlProvider : BaseDbProvider
{
    public MySqlProvider(string connectionString, ILogger logger)
        : base(connectionString, logger) { }

    public override string ProviderName => "MySQL";

    public override async Task<DbConnection> OpenConnectionAsync()
    {
        var conn = new MySqlConnection(ConnectionString);
        await conn.OpenAsync();
        return conn;
    }

    public override string GetSqlDialectHints() => """
        Motor: MySQL / MariaDB
        - Usa LIMIT en lugar de TOP:       SELECT * FROM tabla LIMIT 10
        - Fecha actual:                    NOW()  o  CURDATE()
        - Partes de fecha:                 YEAR(fecha), MONTH(fecha), DAY(fecha)
        - Formatear fecha:                 DATE_FORMAT(fecha, '%Y-%m')
        - Concatenar strings:              CONCAT(a, b)  (no usar +)
        - Conversión de tipo:              CAST(x AS UNSIGNED)
        - Verificar NULL:                  IFNULL(col, valor_defecto)  o  COALESCE
        - Booleano:                        activo = 1  o  activo = 0  (TINYINT)
        - Paginación:                      LIMIT 5 OFFSET 10
        - Strings:                         comillas simples para literales
        - Backticks para nombres de tabla: `tabla`, `columna`
        """;

    public override string GetCreateTablesSql() => """
        CREATE TABLE IF NOT EXISTS departamentos (
            id          INT AUTO_INCREMENT PRIMARY KEY,
            nombre      VARCHAR(100) NOT NULL,
            presupuesto DECIMAL(18,2) NOT NULL
        );

        CREATE TABLE IF NOT EXISTS empleados (
            id              INT AUTO_INCREMENT PRIMARY KEY,
            nombre          VARCHAR(150) NOT NULL,
            cargo           VARCHAR(100) NOT NULL,
            salario         DECIMAL(18,2) NOT NULL,
            departamento_id INT  NOT NULL,
            fecha_contrato  DATE NOT NULL,
            activo          TINYINT(1) NOT NULL DEFAULT 1,
            FOREIGN KEY (departamento_id) REFERENCES departamentos(id)
        );

        CREATE TABLE IF NOT EXISTS productos (
            id        INT AUTO_INCREMENT PRIMARY KEY,
            nombre    VARCHAR(150) NOT NULL,
            categoria VARCHAR(100) NOT NULL,
            precio    DECIMAL(18,2) NOT NULL,
            stock     INT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS clientes (
            id     INT AUTO_INCREMENT PRIMARY KEY,
            nombre VARCHAR(150) NOT NULL,
            ciudad VARCHAR(100) NOT NULL,
            email  VARCHAR(200) NOT NULL
        );

        CREATE TABLE IF NOT EXISTS ventas (
            id          INT AUTO_INCREMENT PRIMARY KEY,
            empleado_id INT     NOT NULL,
            producto_id INT     NOT NULL,
            cantidad    INT     NOT NULL,
            fecha       DATE    NOT NULL,
            total       DECIMAL(18,2) NOT NULL,
            FOREIGN KEY (empleado_id) REFERENCES empleados(id),
            FOREIGN KEY (producto_id) REFERENCES productos(id)
        );
        """;

    public override string GetTableExistsSql(string tableName) =>
        $"SELECT COUNT(*) FROM information_schema.tables WHERE table_name='{tableName}' AND table_schema=DATABASE()";
}
