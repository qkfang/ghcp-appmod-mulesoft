# MovieFunctionApp

.NET 10 isolated-worker Azure Functions app migrated from the MuleSoft
project under [`mulesoft/`](../../mulesoft).

## Endpoints (parity with the Mule `movie-config` API)

| Method | Route                              | Migrated from Mule flow |
|--------|------------------------------------|-------------------------|
| GET    | `/api/movies`                      | `GetMovies`             |
| POST   | `/api/movies/{m_id}?no_tickets=N`  | `BookTickets`           |
| GET    | `/api/swagger/ui`                  | (Swagger UI)            |
| GET    | `/api/openapi/v3.json`             | (OpenAPI v3 document)   |

### Booking pricing tiers (copied verbatim from Mule)

| Tickets | Price per ticket |
|---------|------------------|
| 1 – 5   | 100              |
| 6 – 10  | 90               |
| > 10    | 80               |

If `(m_available - no_tickets) < 0`, a `400 Bad Request` is returned with the
Mule-style payload `{ "error": "avaible tickets is only X but you have ordered Y" }`.

## Data store

The Mule `Database_Config` MySQL connection has been replaced with an
**EF Core in-memory** `MovieDbContext` (per the issue request). To swap in a
real provider later, change the single `AddDbContext` registration in
`Program.cs`.

Seed data is created automatically on startup via `EnsureCreated()`.

## Build & run locally

```bash
dotnet build MovieFunctionApp.csproj
func start            # requires Azure Functions Core Tools v4
```

Then visit `http://localhost:7071/api/swagger/ui`.

## Deploy to Azure

See [`bicep/`](../../bicep) for IaC and the `deploy.ps1` helper.
