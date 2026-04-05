using ChatbotMVC.Data;
using System.Text;

namespace ChatbotMVC.Services;

public class ChatbotService
{
    private readonly DatabaseService _db;
    private readonly GeminiService   _gemini;

    public ChatbotService(DatabaseService db, GeminiService gemini)
    {
        _db    = db;
        _gemini = gemini;
    }

    public async Task<ChatResponse> ProcessAsync(string question)
    {
        var response = new ChatResponse { Question = question };
        try
        {
            // 1. Leer el schema REAL de la BD conectada (se adapta solo)
            var schema = await _db.BuildSchemaPromptAsync();

            // 2. Gemini genera el SQL basado en ese schema
            var sqlResult = await _gemini.GenerateSqlAsync(question, schema);
            if (!sqlResult.Success)
            {
                response.Answer = sqlResult.Explanation;
                response.Type   = ResponseType.Error;
                return response;
            }

            response.Sql     = sqlResult.Sql;
            response.SqlExpl = sqlResult.Explanation;

            // 3. Ejecutar el SQL en la BD
            var qr = await _db.ExecuteQueryAsync(sqlResult.Sql);
            if (!qr.Success)
            {
                response.Answer = $"Error al ejecutar la consulta: {qr.Error}";
                response.Type   = ResponseType.Error;
                return response;
            }

            if (qr.RowCount == 0)
            {
                response.Answer = "No se encontraron resultados.";
                response.Type   = ResponseType.Empty;
                return response;
            }

            response.Table = qr;

            // 4. Gemini interpreta los resultados en lenguaje natural
            var plain = FormatForAI(qr);
            response.Answer = await _gemini.InterpretAsync(question, sqlResult.Sql, plain, qr.RowCount);
            response.Type   = ResponseType.Table;
        }
        catch (Exception ex)
        {
            response.Answer = $"Error: {ex.Message}";
            response.Type   = ResponseType.Error;
        }
        return response;
    }

    private string FormatForAI(QueryResult qr)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(" | ", qr.Columns));
        foreach (var row in qr.Rows.Take(50))
            sb.AppendLine(string.Join(" | ", qr.Columns.Select(c => row[c]?.ToString() ?? "NULL")));
        if (qr.RowCount > 50) sb.AppendLine($"... y {qr.RowCount - 50} filas más.");
        return sb.ToString();
    }
}

public class ChatResponse
{
    public string       Question { get; set; } = "";
    public string       Answer   { get; set; } = "";
    public string?      Sql      { get; set; }
    public string?      SqlExpl  { get; set; }
    public QueryResult? Table    { get; set; }
    public ResponseType Type     { get; set; } = ResponseType.Table;
    public DateTime     Time     { get; set; } = DateTime.Now;
}

public enum ResponseType { Table, Empty, Error }
