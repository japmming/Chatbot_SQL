using ChatbotMVC.Data;
using ChatbotMVC.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

builder.Services.AddSingleton<DatabaseService>();

// 1. CAMBIO IMPORTANTE: Registramos GeminiService como Scoped o Transient.
// Como maneja un HttpClient interno y estados de inicialización por petición,
// no debe ser Singleton en aplicaciones web para evitar colisiones entre usuarios.
builder.Services.AddScoped<GeminiService>();
builder.Services.AddScoped<ChatbotService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

// Conectar a la BD al arrancar
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DatabaseService>();
    await db.InitializeAsync();   // Solo verifica conexión y lee el nombre de la BD

    // 2. SIMPLIFICACIÓN: Eliminamos la extracción manual de variables aquí.
    // El controlador se encargará de inicializar el servicio de forma limpia 
    // en cada petición HTTP (a través de la inyección en el HomeController).
}

app.Run();
