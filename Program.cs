using ChatbotMVC.Data;
using ChatbotMVC.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<DatabaseService>();
builder.Services.AddSingleton<GeminiService>();
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

    // Gemini: inicializar si hay API Key en config
    var gemini = scope.ServiceProvider.GetRequiredService<GeminiService>();
    var apiKey = builder.Configuration["Gemini:ApiKey"]
              ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "";
    if (!string.IsNullOrWhiteSpace(apiKey))
        await gemini.InitializeAsync(apiKey);
}

app.Run();
