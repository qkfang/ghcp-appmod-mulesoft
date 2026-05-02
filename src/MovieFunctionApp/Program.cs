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

builder.Services.AddOpenTelemetry()
    .UseFunctionsWorkerDefaults()
    .UseAzureMonitorExporter();

var app = builder.Build();

// Seed the in-memory store at startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MovieDbContext>();
    MovieDbContext.Seed(db);
}

app.Run();
