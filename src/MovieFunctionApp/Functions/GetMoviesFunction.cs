using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using MovieFunctionApp.Data;

namespace MovieFunctionApp.Functions;

/// <summary>
/// HTTP-triggered Function porting the Mule <c>GetMovies</c> flow:
/// returns all movies that have at least one available ticket.
/// </summary>
public class GetMoviesFunction
{
    private readonly ILogger<GetMoviesFunction> _logger;
    private readonly MovieDbContext _db;

    public GetMoviesFunction(ILogger<GetMoviesFunction> logger, MovieDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    [Function("GetMovies")]
    [OpenApiOperation(operationId: "GetMovies", tags: new[] { "movies" }, Summary = "List available movies", Description = "Returns all movies with m_available > 0 (parity with Mule GetMovies flow).")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(IEnumerable<Movie>), Summary = "List of available movies")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "movies")] HttpRequestData req)
    {
        _logger.LogInformation("GetMovies invoked");

        var movies = await _db.Movies
            .Where(m => m.MAvailable > 0)
            .OrderBy(m => m.MId)
            .ToListAsync();

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(movies);
        return response;
    }
}
