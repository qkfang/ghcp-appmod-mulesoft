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
/// Migrated from Mule flow <c>BookTickets</c> in
/// <c>mulesoft/src/main/mule/implementation.xml</c>.
/// Validates seat availability, inserts an order with tier-based pricing,
/// decrements availability, and returns the new order row.
/// </summary>
public class BookTicketsFunction
{
    private readonly MovieDbContext _db;
    private readonly ILogger<BookTicketsFunction> _logger;

    public BookTicketsFunction(MovieDbContext db, ILogger<BookTicketsFunction> logger)
    {
        _db = db;
        _logger = logger;
    }

    [Function("BookTickets")]
    [OpenApiOperation(operationId: "BookTickets", tags: new[] { "movies" }, Summary = "Book tickets for a movie", Description = "Books a number of tickets for the given movie id. Pricing tiers: 1-5 tickets => 100/seat, 6-10 => 90/seat, >10 => 80/seat.")]
    [OpenApiParameter(name: "m_id", In = ParameterLocation.Path, Required = true, Type = typeof(int), Summary = "Movie id")]
    [OpenApiParameter(name: "no_tickets", In = ParameterLocation.Query, Required = true, Type = typeof(int), Summary = "Number of tickets to book")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(Order), Summary = "The newly created order row")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(BookingErrorResponse), Summary = "Insufficient availability or invalid input")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Summary = "Movie not found")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "movies/{m_id:int}")] HttpRequest req,
        int m_id)
    {
        _logger.LogInformation("BookTickets invoked for m_id={MId}", m_id);

        if (!int.TryParse(req.Query["no_tickets"], out var noTickets) || noTickets <= 0)
        {
            return new BadRequestObjectResult(new BookingErrorResponse
            {
                Error = "Query parameter 'no_tickets' must be a positive integer."
            });
        }

        var movie = await _db.Movies.FirstOrDefaultAsync(m => m.MId == m_id);
        if (movie is null)
        {
            return new NotFoundResult();
        }

        // Mule: <validation:is-true expression="(m_available - no_tickets) >= 0" />
        if (movie.MAvailable - noTickets < 0)
        {
            return new BadRequestObjectResult(new BookingErrorResponse
            {
                Error = $"avaible tickets is only {movie.MAvailable} but you have ordered {noTickets}"
            });
        }

        var order = new Order
        {
            MId = m_id,
            NoTickets = noTickets,
            Price = noTickets * PricePerTicket(noTickets)
        };

        _db.Orders.Add(order);
        movie.MAvailable -= noTickets;
        await _db.SaveChangesAsync();

        return new OkObjectResult(order);
    }

    /// <summary>
    /// Tier pricing copied from the Mule <c>db:insert</c> input parameters:
    /// 1-5 tickets =&gt; 100, 6-10 =&gt; 90, &gt;10 =&gt; 80.
    /// </summary>
    private static int PricePerTicket(int noTickets) => noTickets switch
    {
        <= 5 => 100,
        <= 10 => 90,
        _ => 80
    };
}

/// <summary>Error payload mirroring the Mule on-error-continue handler.</summary>
public class BookingErrorResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;
}
