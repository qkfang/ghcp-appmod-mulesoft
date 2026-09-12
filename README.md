# Book My Show – Azure Function App

Migration of the Mulesoft **Book My Show** movie-ticket-booking application
(in [`mulesoft/`](mulesoft/)) to an **Azure Functions** app built on
**.NET 10 (isolated worker)**, using **EF Core with an in-memory database**
for movie and order data.

## Project structure

```
.
├── bicep/                        # Azure infrastructure as code
│   ├── main.bicep                # Storage, App Insights, Linux App Service plan, Function App
│   ├── main.parameters.json      # Default parameter values
│   └── deploy.ps1                # Minimal `az deployment group create` helper script
├── mulesoft/                     # Original Mulesoft source (reference only, unmodified)
└── src/
    └── MovieFunctionApp/         # .NET 10 isolated-worker Azure Functions app
        ├── Functions/            # HTTP triggers (GetMovies, BookTickets)
        ├── Data/                 # EF Core in-memory DbContext + seed data
        ├── Models/                # Movie, Order, and error response DTOs
        └── Configuration/        # Swagger/OpenAPI document customization
```

## API endpoints

| Method | Route                               | Description                                  |
| ------ | ------------------------------------ | --------------------------------------------- |
| GET    | `/api/movies`                        | List movies that still have tickets available. |
| POST   | `/api/movies/{m_id}?no_tickets=N`    | Book `N` tickets for movie `m_id`.             |

OpenAPI/Swagger metadata is served via `Microsoft.Azure.Functions.Worker.Extensions.OpenApi`
at `/api/swagger/ui` and `/api/swagger.json`.

### Pricing tiers (from the original Mulesoft DataWeave logic)

| Tickets | Unit price |
| ------- | ---------- |
| 1 – 5   | 100        |
| 6 – 10  | 90         |
| 11+     | 80         |

See [`src/MovieFunctionApp/README.md`](src/MovieFunctionApp/README.md) for full
endpoint documentation, response shapes, and local run instructions.

## Local development

```powershell
cd src/MovieFunctionApp
dotnet build
func start
```

The app listens on `http://localhost:7071` by default. No external database is
required — movies and orders are stored in an EF Core in-memory database that
is seeded on startup.

## Deploy to Azure

```powershell
./bicep/deploy.ps1
```

Or run the underlying `az` commands directly:

```powershell
az group create --name rg-moviefunctionapp --location eastus

az deployment group create `
  --resource-group rg-moviefunctionapp `
  --template-file bicep/main.bicep `
  --parameters appName=moviefunctionapp location=eastus appServicePlanSku=B1
```

The Bicep template provisions:

- Storage account (required by the Functions runtime)
- Application Insights
- Linux App Service plan (Dedicated **B1**) — .NET 10 isolated is not supported
  on the legacy Linux Consumption (Y1/Dynamic) plan
- Function App configured for the `dotnet-isolated` worker runtime
  (`linuxFxVersion: DOTNET-ISOLATED|10.0`)

Then deploy the function code itself:

```powershell
cd src/MovieFunctionApp
func azure functionapp publish <functionAppName>
```

> The function app name is printed as an output of the Bicep deployment
> (`functionAppName` / `apiBaseUrl`).

## Configuration

No database connection settings are required — the app uses an EF Core
in-memory store. Application settings configured by Bicep:

| Setting                                  | Description                          |
| ------------------------------------------ | --------------------------------------- |
| `AzureWebJobsStorage`                    | Storage account connection string.   |
| `FUNCTIONS_WORKER_RUNTIME`                | `dotnet-isolated`                     |
| `APPLICATIONINSIGHTS_CONNECTION_STRING`   | Application Insights connection.     |

## Parity notes

A few deliberate, reasoned deviations from the literal Mule behavior were made
for idiomatic REST/API design; see [`src/MovieFunctionApp/README.md`](src/MovieFunctionApp/README.md)
for response shapes. Notably:

- Insufficient-ticket bookings return **HTTP 400** (the original Mule flow's
  `on-error-continue` swallows the validation error and would actually return
  HTTP 200 with an error payload).
- An unknown `m_id` returns **HTTP 404** (not explicitly modeled in the Mule
  flow, which has no not-found handling for a missing movie row).
- A successful booking returns a single JSON `Order` object rather than a
  one-element array (the literal shape of a Mule `db:select` result).
