using ChatbotMVC.Data;
using ChatbotMVC.Models;
using ChatbotMVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChatbotMVC.Controllers;

public class HomeController : Controller
{
    private readonly ChatbotService  _chatbot;
    private readonly GeminiService   _gemini;
    private readonly DatabaseService _db;

    public HomeController(ChatbotService chatbot, GeminiService gemini, DatabaseService db)
    {
        _chatbot = chatbot;
        _gemini  = gemini;
        _db      = db;
    }

    // GET /
    public async Task<IActionResult> Index()
    {
        var vm = new ChatViewModel
        {
            Tables      = await _db.GetSchemaAsync(),
            DbName      = _db.DatabaseName,
            ModelName   = _gemini.CurrentModel,
            GeminiReady = _gemini.IsReady
        };
        return View(vm);
    }

    // POST /Home/Ask  (AJAX)
    [HttpPost]
    public async Task<IActionResult> Ask([FromBody] AskRequest req)
    {
        // Si no está listo y llega una ApiKey, inicializar Gemini ahora
        if (!_gemini.IsReady && !string.IsNullOrWhiteSpace(req.ApiKey))
        {
            try   { await _gemini.InitializeAsync(req.ApiKey); }
            catch (Exception ex)
            { return Json(new AskResponse { Success = false, Error = ex.Message }); }
        }

        if (!_gemini.IsReady)
            return Json(new AskResponse { Success = false, Error = "Ingresa tu API Key de Gemini." });

        if (string.IsNullOrWhiteSpace(req.Question))
            return Json(new AskResponse { Success = false, Error = "La pregunta está vacía." });

        var result = await _chatbot.ProcessAsync(req.Question);

        var resp = new AskResponse
        {
            Success = result.Type != ResponseType.Error,
            Answer  = result.Answer,
            Sql     = result.Sql,
            SqlExpl = result.SqlExpl,
            Type    = result.Type.ToString(),
            Time    = result.Time.ToString("HH:mm:ss")
        };

        if (result.Table != null)
            resp.Table = new TableData
            {
                Columns  = result.Table.Columns,
                RowCount = result.Table.RowCount,
                Rows     = result.Table.Rows
                    .Select(r => r.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString()))
                    .ToList()
            };

        return Json(resp);
    }

    // POST /Home/Initialize  (AJAX — configurar API Key desde la UI)
    [HttpPost]
    public async Task<IActionResult> Initialize([FromBody] InitRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.ApiKey))
            return Json(new { success = false, error = "API Key requerida." });
        try
        {
            var model = await _gemini.InitializeAsync(req.ApiKey);
            return Json(new { success = true, model });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    public IActionResult Error() => View();
}
