using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using MovieFunctionApp.Data;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// In-memory EF Core store standing in for the Mule MySQL Database_Config.
// EF's InMemory provider keeps a process-wide store keyed by database name,
// so a default-scoped DbContext still shares the seeded data and inserted
// orders across function invocations.
builder.Services.AddDbContext<MovieDbContext>(options =>
    options.UseInMemoryDatabase(
        builder.Configuration["MovieDbName"] ?? "MovieDb"));

builder.Services.AddOpenTelemetry()
    .UseFunctionsWorkerDefaults()
    .UseAzureMonitorExporter();

var app = builder.Build();

// Seed the in-memory database on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MovieDbContext>();
    MovieDbContext.Seed(db);
}

app.Run();
