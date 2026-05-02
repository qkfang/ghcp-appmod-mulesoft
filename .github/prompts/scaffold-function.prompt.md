---
mode: agent
description: Scaffold a new HTTP-triggered Azure Function in the .NET isolated worker project.
---

Create a new HTTP-triggered Azure Function in `src/MovieFunctionApp/Functions/`.

Requirements:
- .NET 10 isolated worker model (`Microsoft.Azure.Functions.Worker`)
- Use `[Function]` and `[HttpTrigger]` attributes
- Inject `ILogger<T>` via constructor
- Return `HttpResponseData` with proper status codes and JSON via `WriteAsJsonAsync`
- Validate inputs and return `BadRequest` with `ProblemDetails` on errors
- Add XML doc comments on the function method
- Register any new dependencies in `Program.cs`

Ask the user for:
1. Function name and HTTP route
2. Allowed methods (GET/POST/PUT/DELETE)
3. Auth level (`Anonymous`, `Function`, `Admin`)
4. Request/response shape

After generation, build the project with `dotnet build` and report any errors.
