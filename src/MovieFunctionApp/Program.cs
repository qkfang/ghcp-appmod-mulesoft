using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MovieFunctionApp.Configuration;
using MovieFunctionApp.Data;
using OpenTelemetry;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}

// Customizes the /api/swagger/ui title & description for this API.
builder.Services.AddSingleton<IOpenApiConfigurationOptions, BookMyShowOpenApiConfigurationOptions>();

// No external database is required: movies/orders are kept in an EF Core in-memory
// database, replacing the MySQL Database_Config connector (mulesoft/src/main/mule/global.xml).
builder.Services.AddDbContext<MovieDbContext>(options => options.UseInMemoryDatabase("MoviesDb"));

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MovieDbContext>();
    MovieDbContext.Seed(db);
}

host.Run();
