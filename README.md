# BookMyShow – Azure Function App

Migration of the MuleSoft **BookMyShow** application to an **Azure Functions** app built on **.NET 10 (isolated worker)**, using **EF Core with an in-memory database** for movie and order data.

> 📖 **Demo Guide**: See [Guide.md](Guide.md) for step-by-step demo instructions including GitHub Copilot Chat, Agent, and Cloud Agent workflows.

---

## Project structure

```
.
├── bicep/
│   ├── main.bicep               # Bicep template – all Azure resources
│   └── deploy.ps1               # Minimal deployment helper script (inline vars)
├── mulesoft/                    # Original MuleSoft source (reference only)
└── src/
    └── MovieFunctionApp/
        ├── MovieFunctionApp.csproj  # .NET 10 isolated worker project
        ├── Program.cs               # Host bootstrap & DI
        ├── host.json                # Functions host configuration
        ├── local.settings.json      # Local app settings (gitignored)
        ├── Functions/
        │   └── MoviesFunctions.cs   # HTTP triggers (GetMovies, BookTickets)
        ├── Data/
        │   └── MovieDbContext.cs    # EF Core in-memory DbContext + seed data
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

OpenAPI/Swagger metadata is exposed via the `Microsoft.Azure.Functions.Worker.Extensions.OpenApi` extension. After running locally, the Swagger UI is available at `http://localhost:7275/api/swagger/ui` and the OpenAPI document at `http://localhost:7275/api/swagger.json`.

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

> No external database is required. Movies and orders are stored in an EF Core in-memory database that is seeded on startup in `Program.cs`.

### Build and run

```powershell
cd src/MovieFunctionApp
dotnet build
func start
```

The app listens on `http://localhost:7275` by default (configured in `Properties/launchSettings.json`).

### Quick test

```powershell
# List movies
curl http://localhost:7275/api/movies

# Book 3 tickets for movie 1
curl -X POST "http://localhost:7275/api/movies/1?no_tickets=3"
```

---

## Deploy to Azure

### 1. Provision infrastructure with Bicep

Use the helper script (edit the inline parameters at the top of the script first, or pass them in):

```powershell
./bicep/deploy.ps1 -SubscriptionId <sub-id> -ResourceGroup rg-bookmyshow -Location eastus -BaseName movieapp
```

Or call `az` directly:

```powershell
az group create --name rg-bookmyshow --location eastus

az deployment group create `
  --resource-group rg-bookmyshow `
  --template-file bicep/main.bicep `
  --parameters baseName=movieapp location=eastus
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
