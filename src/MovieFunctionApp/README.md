# MovieFunctionApp

.NET 8 isolated Azure Functions app migrated from the original Mulesoft project in `../mulesoft/`.

## Endpoints

| Method | Route                                      | Description                              |
| ------ | ------------------------------------------ | ---------------------------------------- |
| GET    | `/api/movies`                              | List movies that still have tickets.     |
| POST   | `/api/movies/{m_id}?no_tickets={n}`        | Book `n` tickets for movie `m_id`.       |
| GET    | `/api/swagger/ui`                          | Swagger UI for the API.                  |
| GET    | `/api/swagger.json`                        | OpenAPI 2.0 document (JSON).             |
| GET    | `/api/openapi/{version}.{extension}`       | OpenAPI document for the chosen version. |

Pricing follows the original Mulesoft logic:

* `<= 5` tickets &rarr; 100 per ticket
* `<= 10` tickets &rarr; 90 per ticket
* `> 10` tickets &rarr; 80 per ticket

## Storage

The original MySQL database has been replaced with an **EF Core in-memory database**
(`Microsoft.EntityFrameworkCore.InMemory`). Seed data is loaded on startup from
`Data/MovieDbContext.Seed`. Data is reset every time the worker process restarts.

## Run locally

```pwsh
cd src/MovieFunctionApp
func start
```

Then browse to <http://localhost:7071/api/swagger/ui>.

A `local.settings.json` is required locally; create one with:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  }
}
```

## Deploy to Azure

See `../bicep/main.bicep` and `../deploy.ps1` in the repository root.
