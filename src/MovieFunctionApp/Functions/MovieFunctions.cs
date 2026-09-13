using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using MovieFunctionApp.Data;
using MovieFunctionApp.Models;

namespace MovieFunctionApp.Functions;

/// <summary>
/// HTTP endpoints migrated from the MuleSoft <c>GetMovies</c> and
/// <c>BookTickets</c> flows in
/// <c>mulesoft/src/main/mule/implementation.xml</c>.
/// </summary>
public class MovieFunctions
{
    private readonly ILogger<MovieFunctions> _logger;
    private readonly InMemoryMovieStore _store;

    public MovieFunctions(ILogger<MovieFunctions> logger, InMemoryMovieStore store)
    {
        _logger = logger;
        _store = store;
    }

    /// <summary>
    /// Lists movies with available seats, mirroring
    /// <c>select * from movie_table where m_available &gt; 0</c>.
    /// </summary>
    [Function("GetMovies")]
    [OpenApiOperation(operationId: "GetMovies", tags: new[] { "Movies" }, Summary = "List available movies")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(Movie[]), Description = "Movies with available seats")]
    public async Task<HttpResponseData> GetMovies(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "movies")] HttpRequestData req)
    {
        _logger.LogInformation("Fetching movies with available seats");

        var movies = _store.GetAvailableMovies();

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(movies);
        return response;
    }

    /// <summary>
    /// Books tickets for a movie, mirroring the <c>BookTickets</c> flow:
    /// validates availability, inserts an order, decrements availability,
    /// and returns the newly created order row.
    /// </summary>
    [Function("BookTickets")]
    [OpenApiOperation(operationId: "BookTickets", tags: new[] { "Movies" }, Summary = "Book movie tickets")]
    [OpenApiParameter(name: "m_id", In = ParameterLocation.Path, Required = true, Type = typeof(int), Description = "Movie id")]
    [OpenApiParameter(name: "no_tickets", In = ParameterLocation.Query, Required = true, Type = typeof(int), Description = "Number of tickets to book")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(Order), Description = "The created order, or a BookingError payload when there are insufficient tickets available")]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(BookingError), Description = "Invalid request")]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(BookingError), Description = "Movie not found")]
    public async Task<HttpResponseData> BookTickets(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "movies/{m_id}")] HttpRequestData req,
        string m_id)
    {
        if (!int.TryParse(m_id, out var movieId))
        {
            return await WriteErrorAsync(req, HttpStatusCode.BadRequest, "m_id must be a number");
        }

        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var noTicketsRaw = query["no_tickets"];
        if (!int.TryParse(noTicketsRaw, out var noTickets))
        {
            return await WriteErrorAsync(req, HttpStatusCode.BadRequest, "no_tickets must be a number");
        }

        var movie = _store.GetMovieById(movieId);
        if (movie is null)
        {
            return await WriteErrorAsync(req, HttpStatusCode.NotFound, $"Movie {movieId} not found");
        }

        var order = _store.BookTickets(movie, noTickets);
        if (order is null)
        {
            // Parity with the Mule VALIDATION:INVALID_BOOLEAN on-error-continue
            // handler, which returns a 200 with an error payload (typo preserved).
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new BookingError
            {
                Error = $"avaible tickets is only {movie.MAvailable} but you have ordered {noTickets}",
            });
            return response;
        }

        var ok = req.CreateResponse(HttpStatusCode.OK);
        await ok.WriteAsJsonAsync(order);
        return ok;
    }

    private static async Task<HttpResponseData> WriteErrorAsync(HttpRequestData req, HttpStatusCode statusCode, string message)
    {
        var response = req.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(new BookingError { Error = message });
        return response;
    }
}
