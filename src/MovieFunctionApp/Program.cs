using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MovieFunctionApp.Data;
using OpenTelemetry;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// EF Core in-memory store backing the migrated movie_table / order_table.
// Configurable via the "MovieDb:Name" key (defaults to "MoviesDb").
var dbName = builder.Configuration["MovieDb:Name"] ?? "MoviesDb";
builder.Services.AddDbContext<MovieDbContext>(options => options.UseInMemoryDatabase(dbName));

var otel = builder.Services.AddOpenTelemetry()
    .UseFunctionsWorkerDefaults();

// Only wire up the Azure Monitor exporter when a connection string is provided
// (it's required in Azure but optional locally so the host can start without it).
if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    otel.UseAzureMonitorExporter();
}

var app = builder.Build();

// Seed the in-memory store at startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MovieDbContext>();
    MovieDbContext.Seed(db);
}

app.Run();
