using ChatbotMVC.Data;
using ChatbotMVC.Services;

namespace ChatbotMVC.Models;

public class ChatViewModel
{
    public List<TableInfo> Tables      { get; set; } = new();
    public string          DbName      { get; set; } = "";
    public string          ModelName   { get; set; } = "";
    public bool            GeminiReady { get; set; }
}

public class AskRequest
{
    public string Question { get; set; } = "";
    public string ApiKey   { get; set; } = "";
}

public class AskResponse
{
    public bool       Success  { get; set; }
    public string     Answer   { get; set; } = "";
    public string?    Sql      { get; set; }
    public string?    SqlExpl  { get; set; }
    public TableData? Table    { get; set; }
    public string     Type     { get; set; } = "";
    public string     Time     { get; set; } = "";
    public string?    Error    { get; set; }
}

public class TableData
{
    public List<string>                      Columns  { get; set; } = new();
    public List<Dictionary<string, string?>> Rows     { get; set; } = new();
    public int                               RowCount { get; set; }
}

public class InitRequest { public string ApiKey { get; set; } = ""; }
