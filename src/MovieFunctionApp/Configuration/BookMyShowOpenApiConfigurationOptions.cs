using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.OpenApi.Models;

namespace MovieFunctionApp.Configuration;

/// <summary>
/// Customizes the generated OpenAPI/Swagger document title and description for the
/// migrated Book My Show API (see mulesoft/README.md for the original project).
/// </summary>
public class BookMyShowOpenApiConfigurationOptions : DefaultOpenApiConfigurationOptions
{
    public override OpenApiInfo Info { get; set; } = new OpenApiInfo
    {
        Title = "Book My Show - Movie API",
        Version = "1.0.0",
        Description = "Movie ticket booking API migrated from the Mulesoft BookMyShow application."
    };
}
