using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MovieFunctionApp.Data;
using MovieFunctionApp.Models;

namespace MovieFunctionApp.Functions;

/// <summary>
/// HTTP-triggered functions migrated from the MuleSoft <c>movie</c> API
/// (<c>mulesoft/src/main/mule/interface.xml</c> + <c>implementation.xml</c>).
/// </summary>
public class MoviesFunctions
{
    private readonly MovieDbContext _db;
    private readonly ILogger<MoviesFunctions> _logger;

    public MoviesFunctions(MovieDbContext db, ILogger<MoviesFunctions> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Migrated from the Mule <c>GetMovies</c> flow:
    /// <c>select * from movie_table where m_available &gt; 0</c>.
    /// </summary>
    [Function("GetMovies")]
    [OpenApiOperation(operationId: "GetMovies", tags: new[] { "movies" }, Summary = "List available movies", Description = "Returns all movies with at least one seat available.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(IEnumerable<Movie>), Summary = "Available movies")]
    public async Task<IActionResult> GetMovies(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "movies")] HttpRequest req)
    {
        _logger.LogInformation("GET /api/movies");
        var movies = await _db.Movies
            .Where(m => m.MAvailable > 0)
            .OrderBy(m => m.MId)
            .ToListAsync();
        return new OkObjectResult(movies);
    }

    /// <summary>
    /// Migrated from the Mule <c>BookTickets</c> flow.
    /// Validates seat availability, inserts an order with tiered pricing
    /// (&lt;=5 → x100, &lt;=10 → x90, else x80), and decrements
    /// <c>movie_table.m_available</c>.
    /// </summary>
    [Function("BookTickets")]
    [OpenApiOperation(operationId: "BookTickets", tags: new[] { "movies" }, Summary = "Book tickets for a movie", Description = "Books no_tickets seats for the movie identified by m_id.")]
    [OpenApiParameter(name: "m_id", In = ParameterLocation.Path, Required = true, Type = typeof(int), Summary = "Movie id")]
    [OpenApiParameter(name: "no_tickets", In = ParameterLocation.Query, Required = true, Type = typeof(int), Summary = "Number of tickets to book")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(IEnumerable<Order>), Summary = "Booking succeeded; the created order is returned in an array.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(object), Summary = "Invalid no_tickets parameter")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(object), Summary = "Movie not found")]
    public async Task<IActionResult> BookTickets(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "movies/{m_id:int}")] HttpRequest req,
        int m_id)
    {
        _logger.LogInformation("POST /api/movies/{MId}", m_id);

        if (!int.TryParse(req.Query["no_tickets"], out var noTickets) || noTickets <= 0)
        {
            return new BadRequestObjectResult(new { message = "Query parameter 'no_tickets' must be a positive integer." });
        }

        var movie = await _db.Movies.FirstOrDefaultAsync(m => m.MId == m_id);
        if (movie is null)
        {
            return new NotFoundObjectResult(new { message = $"Movie {m_id} not found" });
        }

        // Mule: validation:is-true ((m_available - no_tickets) >= 0) → on failure return error payload.
        // The Mule on-error-continue handler returns 200 with an error JSON body, so we mirror that.
        // NOTE: the misspelling "avaible" is preserved verbatim from the original Mule flow
        // (implementation.xml → BookTickets → set-payload) for output parity.
        if (movie.MAvailable - noTickets < 0)
        {
            return new ObjectResult(new
            {
                error = $"avaible tickets is only {movie.MAvailable} but you have ordered {noTickets}"
            })
            { StatusCode = StatusCodes.Status200OK };
        }

        var price = noTickets switch
        {
            <= 5 => noTickets * 100,
            <= 10 => noTickets * 90,
            _ => noTickets * 80
        };

        var order = new Order
        {
            MId = m_id,
            NoTickets = noTickets,
            Price = price
        };
        _db.Orders.Add(order);

        movie.MAvailable -= noTickets;
        await _db.SaveChangesAsync();

        // Mule final select: select * from order_table where o_id = MAX(o_id) — return as a single-element array
        // for parity with the Mule output payload.
        return new OkObjectResult(new[] { order });
    }
}
