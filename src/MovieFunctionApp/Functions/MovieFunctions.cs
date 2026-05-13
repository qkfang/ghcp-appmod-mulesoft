using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Logging;
using MovieFunctionApp.Data;
using MovieFunctionApp.Models;

namespace MovieFunctionApp.Functions;

/// <summary>
/// HTTP-triggered functions exposing the movie API. These replace the
/// MuleSoft <c>GetMovies</c> and <c>BookTickets</c> flows.
/// </summary>
public class MovieFunctions
{
    private readonly IMovieRepository _repository;
    private readonly ILogger<MovieFunctions> _logger;

    public MovieFunctions(IMovieRepository repository, ILogger<MovieFunctions> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/movies — returns every movie whose available seat count is &gt; 0.
    /// </summary>
    [Function("GetMovies")]
    [OpenApiOperation(operationId: "GetMovies", tags: new[] { "movies" }, Summary = "List available movies", Description = "Returns every movie whose m_available is greater than zero.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(IEnumerable<Movie>), Summary = "Movies with available seats")]
    public IActionResult GetMovies(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "movies")] HttpRequest req)
    {
        _logger.LogInformation("GetMovies invoked.");
        var movies = _repository.GetAvailableMovies();
        return new OkObjectResult(movies);
    }

    /// <summary>
    /// POST /api/movies/{m_id}?no_tickets=N — books N tickets for the given movie.
    /// </summary>
    [Function("BookTickets")]
    [OpenApiOperation(operationId: "BookTickets", tags: new[] { "movies" }, Summary = "Book tickets", Description = "Books the requested number of tickets for the given movie id.")]
    [OpenApiParameter(name: "m_id", In = ParameterLocation.Path, Required = true, Type = typeof(int), Description = "Movie identifier")]
    [OpenApiParameter(name: "no_tickets", In = ParameterLocation.Query, Required = true, Type = typeof(int), Description = "Number of tickets to book")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(Order), Summary = "The created order, or an error payload when capacity is exceeded")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(object), Summary = "Validation failure")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(object), Summary = "Movie not found")]
    public IActionResult BookTickets(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "movies/{m_id:int}")] HttpRequest req,
        int m_id)
    {
        if (!int.TryParse(req.Query["no_tickets"], out var noTickets) || noTickets <= 0)
        {
            return new BadRequestObjectResult(new { message = "Query parameter 'no_tickets' must be a positive integer." });
        }

        _logger.LogInformation("BookTickets invoked for m_id={MovieId}, no_tickets={NoTickets}.", m_id, noTickets);

        try
        {
            var order = _repository.BookTickets(m_id, noTickets);
            return new OkObjectResult(order);
        }
        catch (KeyNotFoundException ex)
        {
            return new NotFoundObjectResult(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            // Mirrors the Mule on-error-continue branch which returned 200 + error body.
            return new OkObjectResult(new { error = ex.Message });
        }
    }
}
