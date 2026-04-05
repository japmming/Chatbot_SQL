using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace ChatbotMVC.Data.Providers;

/// <summary>Proveedor para Microsoft SQL Server / Azure SQL.</summary>
public class SqlServerProvider : BaseDbProvider
{
    public SqlServerProvider(string connectionString, ILogger logger)
        : base(connectionString, logger) { }

    public override string ProviderName => "SQL Server";

    public override async Task<DbConnection> OpenConnectionAsync()
    {
        var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();
        return conn;
    }

    public override string GetSqlDialectHints() => """
        Motor: Microsoft SQL Server (T-SQL)
        - Usa TOP en lugar de LIMIT:       SELECT TOP 10 * FROM tabla
        - Fecha actual:                    GETDATE()
        - Partes de fecha:                 YEAR(fecha), MONTH(fecha), DAY(fecha)
        - Formatear fecha:                 FORMAT(fecha, 'yyyy-MM')
        - Concatenar strings:              'texto' + columna  o  CONCAT(a, b)
        - Conversión de tipo:              CAST(x AS INT)  o  CONVERT(INT, x)
        - Verificar NULL:                  ISNULL(col, valor_defecto)
        - Bit booleano:                    activo = 1  (no TRUE/FALSE)
        - Paginación:                      OFFSET 10 ROWS FETCH NEXT 5 ROWS ONLY
        """;

    //public override string GetCreateTablesSql() => """
    //    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='departamentos' AND xtype='U')
    //    CREATE TABLE departamentos (
    //        id          INT IDENTITY(1,1) PRIMARY KEY,
    //        nombre      NVARCHAR(100) NOT NULL,
    //        presupuesto DECIMAL(18,2) NOT NULL
    //    );

    //    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='empleados' AND xtype='U')
    //    CREATE TABLE empleados (
    //        id              INT IDENTITY(1,1) PRIMARY KEY,
    //        nombre          NVARCHAR(150) NOT NULL,
    //        cargo           NVARCHAR(100) NOT NULL,
    //        salario         DECIMAL(18,2) NOT NULL,
    //        departamento_id INT  NOT NULL,
    //        fecha_contrato  DATE NOT NULL,
    //        activo          BIT  NOT NULL DEFAULT 1,
    //        FOREIGN KEY (departamento_id) REFERENCES departamentos(id)
    //    );

    //    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='productos' AND xtype='U')
    //    CREATE TABLE productos (
    //        id        INT IDENTITY(1,1) PRIMARY KEY,
    //        nombre    NVARCHAR(150) NOT NULL,
    //        categoria NVARCHAR(100) NOT NULL,
    //        precio    DECIMAL(18,2) NOT NULL,
    //        stock     INT NOT NULL
    //    );

    //    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='clientes' AND xtype='U')
    //    CREATE TABLE clientes (
    //        id     INT IDENTITY(1,1) PRIMARY KEY,
    //        nombre NVARCHAR(150) NOT NULL,
    //        ciudad NVARCHAR(100) NOT NULL,
    //        email  NVARCHAR(200) NOT NULL
    //    );

    //    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ventas' AND xtype='U')
    //    CREATE TABLE ventas (
    //        id          INT IDENTITY(1,1) PRIMARY KEY,
    //        empleado_id INT  NOT NULL,
    //        producto_id INT  NOT NULL,
    //        cantidad    INT  NOT NULL,
    //        fecha       DATE NOT NULL,
    //        total       DECIMAL(18,2) NOT NULL,
    //        FOREIGN KEY (empleado_id) REFERENCES empleados(id),
    //        FOREIGN KEY (producto_id) REFERENCES productos(id)
    //    );
    //    """;

    public override string GetTableExistsSql(string tableName) =>
        $"SELECT COUNT(*) FROM sysobjects WHERE name='{tableName}' AND xtype='U'";
}
