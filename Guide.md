
# Demo Steps

## Existing Mulesoft Project

- Open the `mulesoft/` folder and walk through the original **BookMyShow** app.
- Highlight the key files:
  - `src/main/mule/interface.xml` – HTTP listener and routes
  - `src/main/mule/implementation.xml` – DataWeave business logic (pricing tiers, ticket booking)
  - `src/main/mule/global.xml` – global config
  - `src/main/resources/config.yaml` – app configuration
- Point out the legacy stack: Anypoint Studio, DataWeave, XML flows — vendor-locked runtime, no easy local dev loop.

## GitHub Copilot Chat

- Open Copilot Chat and ask it to explain the existing flows:
  > "Explain what this MuleSoft application does based on the files under `mulesoft/src/main/mule/`."
- Follow up with a migration question:
  > "What would this look like as an Azure Functions app in C#?"
- Show how Chat summarizes the routes, DataWeave transforms, and pricing tiers — a migration blueprint without leaving the editor.

## GitHub Copilot Agent

- Switch to **Agent mode** and prompt:
  > "Migrate the MuleSoft app in `mulesoft/` to a .NET 10 Azure Functions app under `src/MovieFunctionApp/`. Use EF Core in-memory for movies and orders, and preserve the pricing tiers."
- Let the agent scaffold the project and generate `MovieFunctions.cs`, `MovieDbContext.cs`, and the models.
- Review the diff, then build and run locally:
  ```powershell
  cd src/MovieFunctionApp
  dotnet build
  func start
  ```
- Hit the migrated endpoints:
  ```powershell
  curl http://localhost:7071/api/movies
  curl -X POST "http://localhost:7071/api/movies/1?no_tickets=3"
  ```

## GitHub Copilot Cloud Agent

- Assign a follow-up issue to the **Copilot coding agent** on GitHub with the following prompt:
  > "Migrate the MuleSoft project inside the `mulesoft/` folder to be a .net 10 Function App with an API endpoint.
  >
  > - .NET isolated app
  > - Setup Swagger
  > - Don't need a DB, just create an in-memory DB for now
  > - The migrated code should be under the `src/` folder
  > - Make sure to create Bicep under the `bicep/` folder for Azure resources
  > - Create a minimal `.ps1` script to Bicep deploy — `az deployment` with all inline vars"
- Show the agent opening a PR with the migrated Function App, `bicep/main.bicep`, and `deploy.ps1`.
- Review the PR, leave a comment for a tweak, and merge.

## Migrated .Net Function App

- Check out the PR branch locally in VS Code:

- Restore, build, and run the Function App:
  ```powershell
  cd src/MovieFunctionApp
  dotnet restore
  dotnet build
  func start
  ```
- Open the Swagger UI in the browser to explore the migrated endpoints:
  - http://localhost:7071/api/swagger/ui
- Deploy with the generated Bicep:
  ```powershell
  az group create --name rg-bookmyshow --location eastus
  ./bicep/deploy.ps1
  ```
- Test the deployed endpoints:
  ```powershell
  curl https://<func-app>.azurewebsites.net/api/movies
  curl -X POST "https://<func-app>.azurewebsites.net/api/movies/1?no_tickets=5"
  ```
- Wrap up: same business logic, modern stack — .NET 10 isolated worker, OpenAPI, EF Core, IaC, and a clean local dev loop.

