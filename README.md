# BookMyShow – Azure Function App

Migration of the MuleSoft **BookMyShow** application to an **Azure Functions** app built on **.NET 10 (isolated worker)**, using a thread-safe **in-memory repository** for movie and order data.

> 📖 **Demo Guide**: See [Guide.md](Guide.md) for step-by-step demo instructions including GitHub Copilot Chat, Agent, and Cloud Agent workflows.

---

## Project structure

```
.
├── bicep/
│   ├── main.bicep               # Bicep template – all Azure resources
│   └── deploy.ps1               # Deployment helper script (all vars inline)
├── mulesoft/                    # Original MuleSoft source (reference only)
└── src/
    └── MovieFunctionApp/
        ├── MovieFunctionApp.csproj      # .NET 10 isolated worker project
        ├── Program.cs                   # Host bootstrap & DI
        ├── host.json                    # Functions host configuration
        ├── local.settings.json.example  # Template for local app settings
        ├── Functions/
        │   └── MovieFunctions.cs        # HTTP triggers (GetMovies, BookTickets)
        ├── Data/
        │   ├── IMovieRepository.cs      # Repository abstraction
        │   └── InMemoryMovieRepository.cs # Seeded in-memory store
        └── Models/
            ├── Movie.cs
            └── Order.cs
```

---

## API endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `GET`  | `/api/movies` | List all movies with available seats |
| `POST` | `/api/movies/{m_id}?no_tickets=N` | Book N tickets for movie `m_id` |

OpenAPI/Swagger metadata is exposed via the `Microsoft.Azure.Functions.Worker.Extensions.OpenApi` extension. Once the host is running, the following routes are also served:

| Route | Description |
|-------|-------------|
| `GET /api/swagger/ui` | Swagger UI |
| `GET /api/swagger.json` | OpenAPI v2 (Swagger) document |
| `GET /api/openapi/v3.json` | OpenAPI v3 document |

### Pricing tiers (from original MuleSoft logic)

| Tickets | Unit price |
|---------|-----------|
| 1 – 5   | 100       |
| 6 – 10  | 90        |
| 11+     | 80        |

---

## Local development

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local)

> No external database is required. Movies and orders are stored in a thread-safe in-memory repository (`InMemoryMovieRepository`) registered as a singleton in `Program.cs`.

### Build and run

```powershell
# Copy the example local settings file (gitignored)
Copy-Item src/MovieFunctionApp/local.settings.json.example src/MovieFunctionApp/local.settings.json

cd src/MovieFunctionApp
dotnet build
func start
```

The app listens on `http://localhost:7071` by default.

### Quick test

```powershell
# List movies
curl http://localhost:7071/api/movies

# Book 3 tickets for movie 1
curl -X POST "http://localhost:7071/api/movies/1?no_tickets=3"
```

---

## Deploy to Azure

### 1. Provision infrastructure with Bicep

The helper script accepts all parameters inline (no parameter file required):

```powershell
./bicep/deploy.ps1 `
  -SubscriptionId '00000000-0000-0000-0000-000000000000' `
  -ResourceGroup  'rg-bookmyshow' `
  -Location       'australiaeast' `
  -BaseName       'movie'
```

Or call `az` directly:

```powershell
az group create --name rg-bookmyshow --location australiaeast

az deployment group create `
  --resource-group rg-bookmyshow `
  --template-file bicep/main.bicep `
  --parameters baseName=movie location=australiaeast sku=Y1
```

The Bicep template provisions:

- Storage account (Functions runtime)
- Application Insights
- Consumption (Y1 / Dynamic) App Service plan
- Function App configured for the `dotnet-isolated` worker runtime

### 2. Deploy the function code

```powershell
cd src/MovieFunctionApp
func azure functionapp publish <functionAppName>
```

> The function app name is printed as an output of the Bicep deployment.

---

## Configuration

The app does not require any database connection settings — it uses an in-memory store. Standard Functions settings configured by Bicep:

| Setting | Description |
|---------|-------------|
| `AzureWebJobsStorage` | Storage account connection string |
| `FUNCTIONS_WORKER_RUNTIME` | `dotnet-isolated` |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Application Insights connection |
