using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using MovieFunctionApp.Data;
using MovieFunctionApp.Models;

namespace MovieFunctionApp.Functions;

/// <summary>
/// Migrated from Mule flow <c>GetMovies</c> in
/// <c>mulesoft/src/main/mule/implementation.xml</c>.
/// Replaces <c>select * from movie_table where m_available &gt; 0</c>.
/// </summary>
public class GetMoviesFunction
{
    private readonly MovieDbContext _db;
    private readonly ILogger<GetMoviesFunction> _logger;

    public GetMoviesFunction(MovieDbContext db, ILogger<GetMoviesFunction> logger)
    {
        _db = db;
        _logger = logger;
    }

    [Function("GetMovies")]
    [OpenApiOperation(operationId: "GetMovies", tags: new[] { "movies" }, Summary = "List available movies", Description = "Returns all movies that still have at least one available ticket.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(IEnumerable<Movie>), Summary = "Available movies")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "movies")] HttpRequest req)
    {
        _logger.LogInformation("GetMovies invoked");
        var movies = await _db.Movies
            .Where(m => m.MAvailable > 0)
            .OrderBy(m => m.MId)
            .ToListAsync();
        return new OkObjectResult(movies);
    }
}
