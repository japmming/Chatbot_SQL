using ChatbotMVC.Data.Providers;

namespace ChatbotMVC.Data.Seeders;

/// <summary>
/// Inserta datos de ejemplo compatibles con todos los proveedores.
/// Los INSERT usan SQL estándar (sin sintaxis específica de motor).
/// </summary>
public static class DataSeeder
{
    public static async Task SeedAsync(IDbProvider provider)
    {
        await using var conn = await provider.OpenConnectionAsync();

        // Verificar si ya hay datos
        var count = await provider.ExecuteScalarAsync(conn, "SELECT COUNT(*) FROM empleados");
        if (Convert.ToInt64(count ?? 0L) > 0) return;

        var seed = """
            INSERT INTO departamentos (nombre, presupuesto) VALUES
              ('Ventas',           500000),
              ('IT',               800000),
              ('Recursos Humanos', 300000),
              ('Finanzas',         450000),
              ('Marketing',        350000);

            INSERT INTO empleados (nombre, cargo, salario, departamento_id, fecha_contrato, activo) VALUES
              ('Ana Garcia',      'Gerente de Ventas',    8500,  1, '2019-03-15', 1),
              ('Carlos Lopez',    'Desarrollador Senior', 9200,  2, '2020-07-01', 1),
              ('Maria Torres',    'Analista HR',          5800,  3, '2021-01-10', 1),
              ('Pedro Ramirez',   'Contador Senior',      7200,  4, '2018-11-20', 1),
              ('Laura Mendoza',   'Disenadora UX',        6500,  5, '2022-04-05', 1),
              ('Jose Fernandez',  'Vendedor',             4200,  1, '2021-08-12', 1),
              ('Sandra Ruiz',     'DevOps Engineer',      8800,  2, '2020-02-28', 1),
              ('Miguel Castro',   'Analista Financiero',  6800,  4, '2019-09-03', 1),
              ('Lucia Vargas',    'Marketing Manager',    7500,  5, '2021-06-17', 1),
              ('Roberto Diaz',    'Soporte Tecnico',      4500,  2, '2022-10-01', 0),
              ('Camila Herrera',  'Vendedora',            4100,  1, '2023-01-15', 1),
              ('Andres Morales',  'Gerente IT',          11000,  2, '2017-05-20', 1);

            INSERT INTO productos (nombre, categoria, precio, stock) VALUES
              ('Laptop Pro 15',       'Tecnologia', 1200.00,  45),
              ('Mouse Inalambrico',   'Tecnologia',   25.50, 200),
              ('Teclado Mecanico',    'Tecnologia',   89.99, 120),
              ('Monitor 27',          'Tecnologia',  350.00,  30),
              ('Silla Ergonomica',    'Mobiliario',  450.00,  15),
              ('Escritorio Standing', 'Mobiliario',  650.00,  10),
              ('Audifonos BT',        'Tecnologia',  120.00,  80),
              ('Webcam HD',           'Tecnologia',   75.00,  60),
              ('Notebook A4',         'Papeleria',     3.50, 500),
              ('Boligrafo Pack x10',  'Papeleria',     8.00, 300);

            INSERT INTO clientes (nombre, ciudad, email) VALUES
              ('Empresa Alpha S.A.', 'Lima',     'contacto@alpha.com'),
              ('Beta Corp',          'Arequipa', 'ventas@beta.com'),
              ('Gamma Solutions',    'Cusco',    'info@gamma.com'),
              ('Delta Industries',   'Lima',     'admin@delta.com'),
              ('Epsilon Tech',       'Trujillo', 'hola@epsilon.com');

            INSERT INTO ventas (empleado_id, producto_id, cantidad, fecha, total) VALUES
              (1,  1,  3, '2024-01-10', 3600.00),
              (1,  4,  2, '2024-01-15',  700.00),
              (6,  2, 10, '2024-01-20',  255.00),
              (6,  3,  5, '2024-02-01',  449.95),
              (11, 1,  2, '2024-02-10', 2400.00),
              (11, 7,  4, '2024-02-15',  480.00),
              (1,  5,  1, '2024-03-05',  450.00),
              (6,  8,  6, '2024-03-10',  450.00),
              (11, 9, 20, '2024-03-20',   70.00),
              (1,  1,  5, '2024-04-01', 6000.00),
              (6,  4,  1, '2024-04-08',  350.00),
              (11, 3,  3, '2024-04-15',  269.97),
              (1,  6,  2, '2024-05-02', 1300.00),
              (6,  7,  8, '2024-05-10',  960.00),
              (11, 2, 15, '2024-05-18',  382.50);
            """;

        await provider.ExecuteNonQueryAsync(conn, seed);
    }
}
