# MovieFunctionApp

.NET 10 isolated Azure Functions app migrated from the MuleSoft
`mulesoft/` reference project.

## Endpoints

| Method | Route | Description |
|---|---|---|
| GET  | `/api/movies` | List movies with `m_available > 0` (Mule `GetMovies` flow). |
| POST | `/api/movies/{m_id}?no_tickets=N` | Book tickets (Mule `BookTickets` flow). |
| GET  | `/api/swagger/ui` | Swagger UI (provided by `Microsoft.Azure.Functions.Worker.Extensions.OpenApi`). |
| GET  | `/api/openapi/v3.json` | OpenAPI v3 document. |

## Pricing rule (parity with Mule)

| Tickets | Price/ticket |
|---|---|
| ≤ 5  | 100 |
| ≤ 10 | 90  |
| > 10 | 80  |

## Data store

Per the issue requirements, the original MySQL `Database_Config` is replaced
by an EF Core in-memory database (`Microsoft.EntityFrameworkCore.InMemory`).
A small set of seed movies is inserted on startup; orders inserted by
`POST /api/movies/{m_id}` persist for the lifetime of the host process.

## Run locally

```bash
dotnet build
func start
```

(Requires Azure Functions Core Tools v4.)

## Deploy

See `bicep/deploy.ps1` for the minimal Azure deployment script.
