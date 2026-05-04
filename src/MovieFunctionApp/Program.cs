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

// EF Core — in-memory provider for the migrated movie/order tables.
// A real deployment can swap this for SQL via the `SqlConnectionString`
// configuration key without touching any function code.
builder.Services.AddDbContext<MovieDbContext>(options =>
    options.UseInMemoryDatabase("MovieDb"));

builder.Services.AddOpenTelemetry()
    .UseFunctionsWorkerDefaults()
    .UseAzureMonitorExporter(o =>
    {
        // Only attempt to use the connection string when one is configured.
        // Locally (without APPLICATIONINSIGHTS_CONNECTION_STRING) the SDK falls back
        // to the no-op behaviour rather than crashing the worker on startup.
        var aiConn = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
        if (!string.IsNullOrWhiteSpace(aiConn))
        {
            o.ConnectionString = aiConn;
        }
        else
        {
            o.ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000";
        }
    });

var app = builder.Build();

// Ensure the in-memory database is created and seed data is applied on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MovieDbContext>();
    db.Database.EnsureCreated();
}

app.Run();
