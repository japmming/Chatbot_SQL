# 🤖 ChatbotSQL MVC — ASP.NET Core + SQL Server + Google Gemini

Aplicación web ASP.NET Core MVC que permite consultar una base de datos
**SQL Server** en **lenguaje natural** usando **Google Gemini** como motor de IA.

---

## 🏗️ Arquitectura

```
Browser (Chat UI)
      │  AJAX JSON
      ▼
HomeController
      │
      ├─── GeminiService  →  Genera SQL (T-SQL para SQL Server)
      │         │
      │         └──────────→  Interpreta resultados en español
      │
      └─── DatabaseService → Ejecuta SELECT en SQL Server
                                    │
                              EmpresaDB (SQL Server)
```

---

## 📦 Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (local, Express, o Azure SQL)
- API Key de [Google AI Studio](https://aistudio.google.com/app/apikey)

---

## ⚙️ Configuración

### 1. Connection String (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=EmpresaDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

Para SQL Server con usuario y contraseña:
```
Server=localhost;Database=EmpresaDB;User Id=sa;Password=TuPassword;TrustServerCertificate=True;
```

### 2. API Key de Gemini

**Opción A — appsettings.json:**
```json
{
  "Gemini": {
    "ApiKey": "AIzaSy..."
  }
}
```

**Opción B — Variable de entorno:**
```bash
set GEMINI_API_KEY=AIzaSy...
```

**Opción C — Ingresarla en la UI** al abrir la aplicación.

---

## 🚀 Ejecutar

```bash
# Restaurar paquetes
dotnet restore

# Ejecutar
dotnet run

# Abrir en el navegador
# http://localhost:5000
```

La base de datos se crea y se llena con datos de ejemplo automáticamente.

---

## 🗃️ Base de datos (EmpresaDB)

| Tabla           | Descripción                          | Filas |
|-----------------|--------------------------------------|-------|
| `departamentos` | 5 departamentos con presupuesto      | 5     |
| `empleados`     | 12 empleados con salario y cargo     | 12    |
| `productos`     | 10 productos con precio y stock      | 10    |
| `ventas`        | 15 registros de ventas               | 15    |
| `clientes`      | 5 clientes de ejemplo                | 5     |

---

## 💬 Ejemplos de preguntas

```
¿Cuántos empleados hay por departamento?
¿Cuál es el salario promedio general?
¿Qué empleados ganan más de 7000?
¿Cuáles son los 3 productos más vendidos?
¿Cuánto ha vendido cada vendedor?
Muéstrame los productos con stock menor a 30
¿Qué departamento tiene mayor presupuesto?
Total de ventas por mes
Dame los empleados inactivos
```

---

## 🧩 Estructura del proyecto

```
ChatbotMVC/
├── Program.cs                    # Entry point + DI + inicialización
├── appsettings.json              # Config: ConnectionString + Gemini
├── ChatbotMVC.csproj             # Paquetes NuGet
├── Controllers/
│   └── HomeController.cs        # Rutas: Index, Ask (AJAX), Initialize
├── Data/
│   └── DatabaseService.cs       # SQL Server: init, seed, queries, schema
├── Models/
│   └── ViewModels.cs            # DTOs: ChatViewModel, AskRequest/Response
├── Services/
│   ├── GeminiService.cs         # Google Gemini API client
│   └── ChatbotService.cs        # Orquestador: pregunta → SQL → respuesta
└── Views/
    ├── Home/
    │   └── Index.cshtml         # Chat UI + JavaScript
    └── Shared/
        └── _Layout.cshtml       # Layout base
```

---

## 📦 Paquetes NuGet

| Paquete                                    | Uso                    |
|--------------------------------------------|------------------------|
| `Microsoft.Data.SqlClient`                 | Conexión a SQL Server  |
| `Microsoft.EntityFrameworkCore.SqlServer`  | ORM (opcional)         |
| `Newtonsoft.Json`                          | Parseo JSON de Gemini  |
