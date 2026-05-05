using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MovieFunctionApp.Data;
using ParameterLocation = Microsoft.OpenApi.Models.ParameterLocation;

namespace MovieFunctionApp.Functions;

/// <summary>
/// HTTP-triggered functions that port the MuleSoft <c>movie</c> API
/// (<c>interface.xml</c> + <c>implementation.xml</c>) to Azure Functions.
/// </summary>
public class MovieFunctions
{
    private readonly ILogger<MovieFunctions> _logger;
    private readonly MovieDbContext _db;

    public MovieFunctions(ILogger<MovieFunctions> logger, MovieDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    /// <summary>
    /// Mule parity: <c>GetMovies</c> flow —
    /// <c>SELECT * FROM movie_table WHERE m_available &gt; 0</c>.
    /// </summary>
    [Function("GetMovies")]
    [OpenApiOperation(operationId: "GetMovies", tags: new[] { "movies" }, Summary = "List available movies", Description = "Returns all movies with at least one available ticket.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(IEnumerable<Movie>), Summary = "Available movies")]
    public async Task<IActionResult> GetMovies(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "movies")] HttpRequest req)
    {
        _logger.LogInformation("GetMovies invoked");
        var movies = await _db.Movies
            .Where(m => m.MAvailable > 0)
            .ToListAsync();
        return new OkObjectResult(movies);
    }

    /// <summary>
    /// Mule parity: <c>BookTickets</c> flow —
    /// validates availability, inserts an order with tiered pricing
    /// (≤5 → ×100, ≤10 → ×90, otherwise ×80), decrements
    /// <c>movie_table.m_available</c>, and returns the created order row.
    /// On insufficient availability the original Mule flow returns a JSON
    /// error message via <c>VALIDATION:INVALID_BOOLEAN</c>; this is
    /// preserved with HTTP 400.
    /// </summary>
    [Function("BookTickets")]
    [OpenApiOperation(operationId: "BookTickets", tags: new[] { "movies" }, Summary = "Book tickets for a movie", Description = "Books the requested number of tickets for a movie and returns the created order.")]
    [OpenApiParameter(name: "m_id", In = ParameterLocation.Path, Required = true, Type = typeof(int), Description = "Movie identifier")]
    [OpenApiParameter(name: "no_tickets", In = ParameterLocation.Query, Required = true, Type = typeof(int), Description = "Number of tickets to book")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(Order), Summary = "Created order")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(BookingError), Summary = "Insufficient availability or invalid input")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(MessageResponse), Summary = "Movie not found")]
    public async Task<IActionResult> BookTickets(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "movies/{m_id}")] HttpRequest req,
        string m_id)
    {
        _logger.LogInformation("BookTickets invoked for m_id={MId}", m_id);

        if (!int.TryParse(m_id, out var movieId))
        {
            return new BadRequestObjectResult(new MessageResponse { Message = "Bad request" });
        }

        var noTicketsRaw = req.Query["no_tickets"].ToString();
        if (!int.TryParse(noTicketsRaw, out var noTickets) || noTickets <= 0)
        {
            return new BadRequestObjectResult(new MessageResponse { Message = "Bad request" });
        }

        var movie = await _db.Movies.FirstOrDefaultAsync(m => m.MId == movieId);
        if (movie is null)
        {
            return new NotFoundObjectResult(new MessageResponse { Message = "Resource not found" });
        }

        if (movie.MAvailable - noTickets < 0)
        {
            // Mirrors the Mule on-error-continue payload for VALIDATION:INVALID_BOOLEAN.
            // The misspelling "avaible" is preserved verbatim from the original
            // MuleSoft flow (mulesoft/src/main/mule/implementation.xml) for strict
            // response-shape parity; do not "correct" it.
            return new BadRequestObjectResult(new BookingError
            {
                Error = $"avaible tickets is only {movie.MAvailable} but you have ordered {noTickets}"
            });
        }

        var price = CalculatePrice(noTickets);

        var order = new Order
        {
            MId = movieId,
            NoTickets = noTickets,
            Price = price
        };
        _db.Orders.Add(order);

        movie.MAvailable -= noTickets;
        await _db.SaveChangesAsync();

        return new OkObjectResult(order);
    }

    /// <summary>
    /// Tiered pricing rule from the Mule <c>BookTickets</c> insert:
    /// <list type="bullet">
    ///   <item><description>≤ 5 tickets → 100 per ticket</description></item>
    ///   <item><description>≤ 10 tickets → 90 per ticket</description></item>
    ///   <item><description>otherwise → 80 per ticket</description></item>
    /// </list>
    /// </summary>
    internal static int CalculatePrice(int noTickets)
    {
        if (noTickets <= 5) return noTickets * 100;
        if (noTickets <= 10) return noTickets * 90;
        return noTickets * 80;
    }
}
