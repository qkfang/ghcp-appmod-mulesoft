using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using MovieFunctionApp.Data;
using MovieFunctionApp.Models;

namespace MovieFunctionApp.Functions;

/// <summary>
/// HTTP-triggered functions migrated from the Mule <c>movie-config</c> API
/// (mulesoft/src/main/mule/interface.xml and implementation.xml):
/// <c>GetMovies</c> and <c>BookTickets</c>.
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
    /// Returns the movies that still have tickets available, equivalent to the Mule
    /// <c>GetMovies</c> flow's <c>select * from movie_table where m_available &gt; 0</c>.
    /// </summary>
    [Function("GetMovies")]
    [OpenApiOperation(operationId: "getMovies", tags: new[] { "movies" }, Summary = "Get all available movies", Description = "Returns the list of movies that still have tickets available.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(IEnumerable<Movie>), Summary = "List of available movies")]
    public async Task<IActionResult> GetMovies(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "movies")] HttpRequest req)
    {
        _logger.LogInformation("GetMovies invoked");

        var movies = await _db.Movies
            .AsNoTracking()
            .Where(m => m.MAvailable > 0)
            .ToListAsync();

        return new OkObjectResult(movies);
    }

    /// <summary>
    /// Books tickets for a movie, equivalent to the Mule <c>BookTickets</c> flow: validates
    /// availability, calculates the tiered price, inserts an order and decrements availability.
    /// </summary>
    [Function("BookTickets")]
    [OpenApiOperation(operationId: "bookTickets", tags: new[] { "movies" }, Summary = "Book tickets for a movie", Description = "Books a number of tickets for the specified movie and returns the created order.")]
    [OpenApiParameter(name: "m_id", In = ParameterLocation.Path, Required = true, Type = typeof(int), Summary = "Movie identifier")]
    [OpenApiParameter(name: "no_tickets", In = ParameterLocation.Query, Required = true, Type = typeof(int), Summary = "Number of tickets to book")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(Order), Summary = "The created order")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(BookingError), Summary = "Not enough tickets available")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(ApiMessage), Summary = "Movie not found")]
    public async Task<IActionResult> BookTickets(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "movies/{m_id:int}")] HttpRequest req,
        int m_id)
    {
        _logger.LogInformation("BookTickets invoked for m_id={MId}", m_id);

        if (!int.TryParse(req.Query["no_tickets"], out var noTickets) || noTickets <= 0)
        {
            return new BadRequestObjectResult(new ApiMessage { Message = "Bad request" });
        }

        var movie = await _db.Movies.FirstOrDefaultAsync(m => m.MId == m_id);
        if (movie is null)
        {
            return new NotFoundObjectResult(new ApiMessage { Message = "Resource not found" });
        }

        if (movie.MAvailable - noTickets < 0)
        {
            // Mirrors the "avaible tickets is only X but you have ordered Y" message
            // (typo preserved) from the VALIDATION:INVALID_BOOLEAN error handler.
            return new BadRequestObjectResult(new BookingError
            {
                Error = $"avaible tickets is only {movie.MAvailable} but you have ordered {noTickets}"
            });
        }

        var order = new Order
        {
            MId = m_id,
            NoTickets = noTickets,
            Price = CalculatePrice(noTickets)
        };

        _db.Orders.Add(order);
        movie.MAvailable -= noTickets;
        await _db.SaveChangesAsync();

        return new OkObjectResult(order);
    }

    /// <summary>
    /// Tiered pricing from the original DataWeave: &lt;=5 tickets @100 each, &lt;=10 @90 each,
    /// otherwise @80 each (mulesoft/src/main/mule/implementation.xml, "Insert" db:input-parameters).
    /// </summary>
    internal static decimal CalculatePrice(int noTickets)
    {
        if (noTickets <= 5)
        {
            return noTickets * 100m;
        }

        if (noTickets <= 10)
        {
            return noTickets * 90m;
        }

        return noTickets * 80m;
    }
}
