namespace ChatbotMVC.Data.Providers;

/// <summary>
/// Fábrica que lee la configuración y retorna el proveedor correcto.
/// Para agregar un nuevo motor: crear un nuevo Provider e incluirlo aquí.
///
/// Configuración en appsettings.json:
/// {
///   "Database": {
///     "Provider": "SqlServer",          // SqlServer | PostgreSQL | MySQL | SQLite
///     "ConnectionString": "..."
///   }
/// }
/// </summary>
public static class DbProviderFactory
{
    private static readonly HashSet<string> SupportedProviders = new(StringComparer.OrdinalIgnoreCase)
    {
        "SqlServer", "PostgreSQL", "MySQL", "SQLite"
    };

    public static IDbProvider Create(IConfiguration config, ILogger logger)
    {
        var provider = config["Database:Provider"]
            ?? throw new InvalidOperationException(
                "Falta 'Database:Provider' en appsettings.json. " +
                $"Valores permitidos: {string.Join(", ", SupportedProviders)}");

        var connStr = config["Database:ConnectionString"]
            ?? config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Falta 'Database:ConnectionString' o 'ConnectionStrings:DefaultConnection'.");

        logger.LogInformation("Iniciando proveedor de BD: {Provider}", provider);

        return provider.ToLower() switch
        {
            "sqlserver"  => new SqlServerProvider(connStr, logger),
            //"postgresql" => new PostgreSqlProvider(connStr, logger),
            //"mysql"      => new MySqlProvider(connStr, logger),
            //"sqlite"     => new SQLiteProvider(connStr, logger),
            _ => throw new InvalidOperationException(
                $"Proveedor '{provider}' no soportado. " +
                $"Opciones: {string.Join(", ", SupportedProviders)}")
        };
    }
}
