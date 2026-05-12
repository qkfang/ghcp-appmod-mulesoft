using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MovieFunctionApp.Data;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// In-memory EF Core DB replaces the Mule MySQL Database_Config.
// To swap in a real provider later, change this single registration.
builder.Services.AddDbContext<MovieDbContext>(options =>
    options.UseInMemoryDatabase("MovieBookingDb"));

var app = builder.Build();

// Ensure seed data is materialized for the in-memory provider.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MovieDbContext>();
    db.Database.EnsureCreated();
}

app.Run();
