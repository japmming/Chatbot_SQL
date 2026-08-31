using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ChatbotMVC.Services;

public class GeminiService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<GeminiService> _logger;
    private string _model = "";
    private string _apiKey = "";
    private bool _ready = false;

    private const string ApiBase = "https://generativelanguage.googleapis.com/v1beta/models";

    public string CurrentModel => _model;
    public bool IsReady => _ready;

    public GeminiService(IHttpClientFactory factory, IConfiguration config, ILogger<GeminiService> logger)
    {
        _http = factory.CreateClient();
        _config = config;
        _logger = logger;

        // Lee la clave automáticamente desde appsettings, User Secrets o Variables de Entorno
        _apiKey = _config["Gemini:ApiKey"] ?? "";
    }

    public async Task<string> InitializeAsync(string? explicitApiKey = null)
    {
        // 1. Si nos pasaron una clave desde la UI, la asignamos
        if (!string.IsNullOrWhiteSpace(explicitApiKey))
        {
            _apiKey = explicitApiKey;
        }

        // 2. Si no hay clave en ningún lado, disparamos el error
        if (string.IsNullOrWhiteSpace(_apiKey) || _apiKey == "TU_API_KEY_AQUI")
        {
            throw new Exception("La API Key de Gemini no está configurada en el backend ni fue ingresada en la UI.");
        }

        _model = await DetectModelAsync();
        _ready = true;
        _logger.LogInformation("Gemini listo. Modelo: {Model}", _model);
        return _model;
    }


    // ── Paso 1: generar SQL ──────────────────────────────────────────────

    public async Task<SqlGenerationResult> GenerateSqlAsync(string question, string schemaPrompt)
    {
        EnsureReady();
        var prompt =
             "Eres un experto en SQL Server. Convierte la pregunta en una consulta T-SQL válida.\n\n" +
             schemaPrompt + "\n\n" +
             "REGLAS:\n" +
             "1. Responde SOLO con JSON sin markdown:\n" +
             "{\"sql\": \"SELECT ...\", \"explanation\": \"breve explicación\"}\n" +
             "2. Solo SELECT. Nunca INSERT, UPDATE, DELETE, DROP.\n" +
             "3. Si no puedes responder con los datos disponibles:\n" +
             "{\"sql\": null, \"explanation\": \"motivo\"}\n\n" +
             "Pregunta: " + question;

        var raw = await CallAsync(prompt);
        return ParseSql(raw);
    }

    // ── Paso 2: interpretar resultados ───────────────────────────────────

    public async Task<string> InterpretAsync(string question, string sql, string data, int rows)
    {
        EnsureReady();
        var prompt = $"""
            Eres un asistente de análisis de datos. Responde en español de forma clara y concisa.
            - Responde directamente la pregunta.
            - Menciona números con claridad.
            - Si la lista es larga, resume los más relevantes.
            - Máximo 3 párrafos.

            Pregunta: {question}
            SQL: {sql}
            Resultados ({rows} fila(s)):
            {data}
            """;

        return await CallAsync(prompt);
    }

    // ── HTTP ─────────────────────────────────────────────────────────────

    private async Task<string> CallAsync(string prompt)
    {
        var url = $"{ApiBase}/{_model}:generateContent?key={_apiKey}";
        var payload = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new { temperature = 0.1, maxOutputTokens = 1024 }
        };

        var res = await _http.PostAsync(url,
            new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"));
        var body = await res.Content.ReadAsStringAsync();

        if (!res.IsSuccessStatusCode)
        {
            var msg = JObject.Parse(body)["error"]?["message"]?.ToString() ?? body;
            throw new Exception($"Gemini {(int)res.StatusCode}: {msg}");
        }

        return JObject.Parse(body)["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString()
               ?? throw new Exception("Respuesta vacía de Gemini.");
    }

    // ── Auto-detección de modelo ──────────────────────────────────────────

    private async Task<string> DetectModelAsync()
    {
        var preferred = _config.GetSection("Gemini:PreferredModels").Get<string[]>()
                     ?? new[] { "gemini-2.0-flash-lite", "gemini-2.0-flash", "gemini-2.5-flash" };

        var res = await _http.GetAsync($"{ApiBase}?key={_apiKey}");
        var body = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode) throw new Exception("No se pudo conectar con Gemini.");

        var models = JObject.Parse(body)["models"] as JArray ?? new JArray();
        var available = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var m in models)
        {
            var name = m["name"]?.ToString()?.Replace("models/", "") ?? "";
            var methods = m["supportedGenerationMethods"] as JArray;
            if (methods?.Any(x => x.ToString() == "generateContent") ?? false)
                available.Add(name);
        }

        foreach (var p in preferred)
            if (available.Contains(p)) return p;

        return available.FirstOrDefault()
               ?? throw new Exception("No hay modelos Gemini disponibles.");
    }

    // ── Parse SQL ────────────────────────────────────────────────────────

    private SqlGenerationResult ParseSql(string raw)
    {
        try
        {
            var clean = raw.Replace("```json", "").Replace("```", "").Trim();
            var s = clean.IndexOf('{'); var e = clean.LastIndexOf('}');
            if (s < 0 || e < 0) return SqlGenerationResult.Fail("No se pudo parsear la respuesta.");

            var obj = JObject.Parse(clean[s..(e + 1)]);
            var sql = obj["sql"]?.ToString();
            var expl = obj["explanation"]?.ToString() ?? "";

            if (string.IsNullOrWhiteSpace(sql) || sql == "null")
                return SqlGenerationResult.Fail(expl);

            return SqlGenerationResult.Ok(sql.Trim(), expl);
        }
        catch { return SqlGenerationResult.Fail("Error al parsear respuesta de Gemini."); }
    }

    private void EnsureReady()
    {
        if (!_ready) throw new Exception("Gemini no está configurado. Ingresa tu API Key.");
    }
}

public class SqlGenerationResult
{
    public bool Success { get; private set; }
    public string Sql { get; private set; } = "";
    public string Explanation { get; private set; } = "";

    public static SqlGenerationResult Ok(string sql, string expl) =>
        new() { Success = true, Sql = sql, Explanation = expl };
    public static SqlGenerationResult Fail(string reason) =>
        new() { Success = false, Explanation = reason };
}
