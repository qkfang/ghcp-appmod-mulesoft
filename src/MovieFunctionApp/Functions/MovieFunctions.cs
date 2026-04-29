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

public class MovieFunctions
{
    private readonly ILogger<MovieFunctions> _logger;
    private readonly MovieDbContext _db;

    public MovieFunctions(ILogger<MovieFunctions> logger, MovieDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    [Function("GetMovies")]
    [OpenApiOperation(operationId: "getMovies", tags: new[] { "movies" }, Summary = "Get all available movies", Description = "Returns the list of movies that still have tickets available.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(IEnumerable<Movie>), Summary = "List of available movies")]
    public async Task<IActionResult> GetMovies(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "movies")] HttpRequest req)
    {
        _logger.LogInformation("GetMovies invoked");
        var movies = await _db.Movies
            .Where(m => m.M_Available > 0)
            .ToListAsync();
        return new OkObjectResult(movies);
    }

    [Function("BookTickets")]
    [OpenApiOperation(operationId: "bookTickets", tags: new[] { "movies" }, Summary = "Book tickets for a movie", Description = "Books a number of tickets for the specified movie and returns the created order.")]
    [OpenApiParameter(name: "m_id", In = ParameterLocation.Path, Required = true, Type = typeof(int), Summary = "Movie identifier")]
    [OpenApiParameter(name: "no_tickets", In = ParameterLocation.Query, Required = true, Type = typeof(int), Summary = "Number of tickets to book")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(Order), Summary = "The created order")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(BookingError), Summary = "Booking failed - not enough tickets or invalid input")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Summary = "Movie not found")]
    public async Task<IActionResult> BookTickets(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "movies/{m_id:int}")] HttpRequest req,
        int m_id)
    {
        _logger.LogInformation("BookTickets invoked for m_id={MId}", m_id);

        if (!int.TryParse(req.Query["no_tickets"], out var noTickets) || noTickets <= 0)
        {
            return new BadRequestObjectResult(new BookingError { Error = "Query parameter 'no_tickets' is required and must be a positive integer." });
        }

        var movie = await _db.Movies.FirstOrDefaultAsync(m => m.M_Id == m_id);
        if (movie is null)
        {
            return new NotFoundResult();
        }

        if (movie.M_Available - noTickets < 0)
        {
            return new BadRequestObjectResult(new BookingError
            {
                Error = $"avaible tickets is only {movie.M_Available} but you have ordered {noTickets}"
            });
        }

        var price = CalculatePrice(noTickets);

        var order = new Order
        {
            M_Id = m_id,
            No_Tickets = noTickets,
            Price = price
        };
        _db.Orders.Add(order);
        movie.M_Available -= noTickets;
        await _db.SaveChangesAsync();

        return new OkObjectResult(order);
    }

    internal static decimal CalculatePrice(int noTickets)
    {
        if (noTickets <= 5) return noTickets * 100m;
        if (noTickets <= 10) return noTickets * 90m;
        return noTickets * 80m;
    }
}
