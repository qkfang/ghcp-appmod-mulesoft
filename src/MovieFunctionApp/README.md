# MovieFunctionApp

.NET 10 isolated-worker Azure Functions app migrated from the original Mulesoft
"Book My Show" project in [`../../mulesoft`](../../mulesoft/). See the repository
root [`README.md`](../../README.md) for the overall project layout and Azure
deployment instructions.

## Endpoints

| Method | Route                                 | Description                                             |
| ------ | -------------------------------------- | -------------------------------------------------------- |
| GET    | `/api/movies`                          | List movies that still have tickets available.            |
| POST   | `/api/movies/{m_id}?no_tickets={n}`    | Book `n` tickets for movie `m_id`.                        |
| GET    | `/api/swagger/ui`                      | Swagger UI.                                               |
| GET    | `/api/swagger.json`                    | OpenAPI (Swagger 2.0) document.                           |
| GET    | `/api/openapi/{version}.{extension}`   | OpenAPI/Swagger document for the requested version/format.|

### `GET /api/movies`

Returns every movie where `m_available > 0`, equivalent to the Mule `GetMovies`
flow's `select * from movie_table where m_available > 0`:

```json
[
  { "m_id": 1, "m_name": "The Shawshank Redemption", "m_available": 48 }
]
```

### `POST /api/movies/{m_id}?no_tickets={n}`

Books `n` tickets for movie `m_id`, following the original Mule `BookTickets` flow:

| Condition                              | Status | Body                                                                 |
| --------------------------------------- | ------ | --------------------------------------------------------------------- |
| Success                                 | 200    | The created order: `{ "o_id", "m_id", "no_tickets", "price" }`         |
| `no_tickets` missing/not a positive int | 400    | `{ "message": "Bad request" }`                                        |
| Movie `m_id` not found                  | 404    | `{ "message": "Resource not found" }`                                 |
| Not enough tickets available            | 400    | `{ "error": "avaible tickets is only {available} but you have ordered {requested}" }` (typo preserved from the original Mule error message) |

Pricing follows the original DataWeave tiers (total price, not per-ticket):

* `<= 5` tickets &rarr; 100 per ticket
* `<= 10` tickets &rarr; 90 per ticket
* `> 10` tickets &rarr; 80 per ticket

## Storage

The original MySQL `Database_Config` connector (`mulesoft/src/main/mule/global.xml`)
has been replaced with an **EF Core in-memory database**
(`Microsoft.EntityFrameworkCore.InMemory`) — no external database is required.
Seed data (5 movies) is loaded on startup from `Data/MovieDbContext.Seed`.
Data resets whenever the worker process restarts.

## Run locally

Prerequisites: [.NET 10 SDK](https://dotnet.microsoft.com/download) and
[Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local).

```powershell
cd src/MovieFunctionApp
dotnet build
func start
```

Then browse to <http://localhost:7071/api/swagger/ui>.

A `local.settings.json` (git-ignored) is required; the one included in this
project is already configured with dev-safe defaults:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  }
}
```

### Quick test

```powershell
curl http://localhost:7071/api/movies
curl -X POST "http://localhost:7071/api/movies/1?no_tickets=3"
```

## Deploy to Azure

See [`../../bicep`](../../bicep/) for the Bicep template and deployment script.
