using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using MovieFunctionApp.Data;
using MovieFunctionApp.Models;

namespace MovieFunctionApp.Functions;

/// <summary>
/// HTTP-triggered functions migrated from the MuleSoft <c>movie</c> API.
/// Mirrors the routes exposed by the original APIKit router in
/// <c>mulesoft/src/main/mule/interface.xml</c>:
/// <list type="bullet">
///   <item><c>GET  /api/movies</c>           → <c>GetMovies</c> Mule flow</item>
///   <item><c>POST /api/movies/{m_id}</c>    → <c>BookTickets</c> Mule flow</item>
/// </list>
/// </summary>
public class MovieFunctions
{
    private readonly MovieDbContext _db;
    private readonly ILogger<MovieFunctions> _logger;

    public MovieFunctions(MovieDbContext db, ILogger<MovieFunctions> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// GET <c>/api/movies</c> — return all movies that have at least one ticket
    /// available. Mirrors the Mule <c>GetMovies</c> flow:
    /// <code>select * from movie_table where m_available &gt; 0</code>.
    /// </summary>
    [Function("GetMovies")]
    [OpenApiOperation(operationId: "GetMovies", tags: new[] { "movies" }, Summary = "List movies with available tickets")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(IEnumerable<Movie>), Summary = "Available movies")]
    public async Task<IActionResult> GetMovies(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "movies")] HttpRequest req)
    {
        _logger.LogInformation("GetMovies invoked");

        var movies = await _db.Movies
            .Where(m => m.MAvailable > 0)
            .OrderBy(m => m.MId)
            .ToListAsync();

        return new OkObjectResult(movies);
    }

    /// <summary>
    /// POST <c>/api/movies/{m_id}?no_tickets=N</c> — book <c>N</c> tickets for the
    /// movie identified by <c>m_id</c>. Mirrors the Mule <c>BookTickets</c> flow:
    /// validates availability, inserts a row in <c>order_table</c>, decrements
    /// <c>m_available</c> in <c>movie_table</c>, and returns the created order.
    /// </summary>
    [Function("BookTickets")]
    [OpenApiOperation(operationId: "BookTickets", tags: new[] { "movies" }, Summary = "Book tickets for a movie")]
    [OpenApiParameter(name: "m_id", In = ParameterLocation.Path, Required = true, Type = typeof(int), Summary = "Movie id")]
    [OpenApiParameter(name: "no_tickets", In = ParameterLocation.Query, Required = true, Type = typeof(int), Summary = "Number of tickets to book")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(Order), Summary = "Created order")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(BookingError), Summary = "Booking failed (invalid params or insufficient availability)")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Summary = "Movie not found")]
    public async Task<IActionResult> BookTickets(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "movies/{m_id}")] HttpRequest req,
        string m_id)
    {
        _logger.LogInformation("BookTickets invoked for m_id={MId}", m_id);

        if (!int.TryParse(m_id, out var movieId))
        {
            return new BadRequestObjectResult(new BookingError { Error = "m_id must be an integer" });
        }

        if (!int.TryParse(req.Query["no_tickets"], out var noTickets) || noTickets <= 0)
        {
            return new BadRequestObjectResult(new BookingError { Error = "no_tickets query parameter is required and must be a positive integer" });
        }

        var movie = await _db.Movies.FirstOrDefaultAsync(m => m.MId == movieId);
        if (movie is null)
        {
            return new NotFoundResult();
        }

        // Mirrors Mule validation:is-true — if requested exceeds availability, return the
        // VALIDATION:INVALID_BOOLEAN error payload from the original flow.
        if (movie.MAvailable - noTickets < 0)
        {
            return new BadRequestObjectResult(new BookingError
            {
                Error = $"avaible tickets is only {movie.MAvailable} but you have ordered {noTickets}"
            });
        }

        // Pricing rules from the Mule DataWeave expression in BookTickets.
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
    /// Pricing tiers preserved from the Mule DataWeave logic:
    /// <c>&lt;=5 → 100/ticket</c>, <c>&lt;=10 → 90/ticket</c>, otherwise <c>80/ticket</c>.
    /// </summary>
    internal static int CalculatePrice(int noTickets) =>
        noTickets <= 5 ? noTickets * 100 :
        noTickets <= 10 ? noTickets * 90 :
        noTickets * 80;
}

