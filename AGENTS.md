# AGENT.md

Guidance for AI coding agents working in this repository.

## What this repo is
A migration workspace that ports a **MuleSoft (Mule 4)** integration application to an **Azure Functions (.NET 10, isolated worker)** application, with Bicep IaC for Azure.

## Layout
| Path | Purpose |
|---|---|
| `mulesoft/` | Original Mule 4 source — **read-only reference**. Do not modify. |
| `src/MovieFunctionApp/` | Target .NET isolated-worker Functions app. |
| `bicep/` | Azure infrastructure (`main.bicep`, parameters, `deploy.ps1`). |
| `docs/`, `Guide.md`, `README.md` | Migration docs. |

## Build & run
```powershell
# Build the function app
dotnet build src/MovieFunctionApp/MovieFunctionApp.csproj

# Run locally
cd src/MovieFunctionApp
func start

# Deploy infra
cd bicep
./deploy.ps1
```

## Conventions
- Follow [.github/instructions/dotnet-functions.instructions.md](.github/instructions/dotnet-functions.instructions.md) for all C# code under `src/MovieFunctionApp/`.
- Use `Microsoft.Azure.Functions.Worker` (isolated). Do not switch to in-process.
- Externalize config via `IConfiguration`; secrets via Key Vault + Managed Identity.
- Keep the `mulesoft/` tree pristine — it's the source of truth for migration parity.

## When migrating a Mule artifact
1. Switch to the **MuleSoft Migrator** chat mode (`.github/chatmodes/mulesoft-migrator.chatmode.md`).
2. Identify the source artifact (flow XML or `.dwl`).
3. Use the matching prompt:
   - New endpoint → `scaffold-function.prompt.md`
   - DataWeave transform → `dataweave-to-csharp.prompt.md`
   - Database access → `add-ef-data-access.prompt.md`
4. Build with `dotnet build` and verify with `func start` before declaring done.

## Pull request expectations
- `dotnet build` succeeds with no warnings introduced.
- New public types have XML doc comments.
- No secrets, connection strings, or personal subscription IDs committed.
- Bicep changes validated with `az deployment group validate` or `what-if`.
