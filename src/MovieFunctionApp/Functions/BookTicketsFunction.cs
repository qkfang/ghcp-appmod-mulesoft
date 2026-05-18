using System.Net;
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
/// HTTP-triggered Function porting the Mule <c>BookTickets</c> flow.
/// Books N tickets for the given movie, computes a tiered price
/// (≤5 ⇒ x100, ≤10 ⇒ x90, else ⇒ x80) and returns the created order.
/// </summary>
public class BookTicketsFunction
{
    private readonly ILogger<BookTicketsFunction> _logger;
    private readonly MovieDbContext _db;

    public BookTicketsFunction(ILogger<BookTicketsFunction> logger, MovieDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    [Function("BookTickets")]
    [OpenApiOperation(operationId: "BookTickets", tags: new[] { "movies" }, Summary = "Book tickets for a movie", Description = "Decrements available seats and records an order. Parity with Mule BookTickets flow.")]
    [OpenApiParameter(name: "m_id", In = ParameterLocation.Path, Required = true, Type = typeof(int), Description = "Movie identifier")]
    [OpenApiParameter(name: "no_tickets", In = ParameterLocation.Query, Required = true, Type = typeof(int), Description = "Number of tickets to book")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(Order), Summary = "The newly-created order")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(BookingError), Summary = "Insufficient seats or invalid input")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(BookingError), Summary = "Movie not found")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "movies/{m_id}")] HttpRequestData req,
        int m_id)
    {
        _logger.LogInformation("BookTickets invoked for m_id={MovieId}", m_id);

        // Parse no_tickets query parameter.
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var noTicketsRaw = query["no_tickets"];
        if (!int.TryParse(noTicketsRaw, out var noTickets) || noTickets <= 0)
        {
            return await WriteJsonAsync(req, HttpStatusCode.BadRequest,
                new BookingError { Error = "Query parameter 'no_tickets' must be a positive integer." });
        }

        var movie = await _db.Movies.FirstOrDefaultAsync(m => m.MId == m_id);
        if (movie is null)
        {
            return await WriteJsonAsync(req, HttpStatusCode.NotFound,
                new BookingError { Error = $"Movie with m_id {m_id} not found." });
        }

        // Mule validation:is-true — if available - requested < 0, fail with the error payload.
        if (movie.MAvailable - noTickets < 0)
        {
            return await WriteJsonAsync(req, HttpStatusCode.BadRequest, new BookingError
            {
                Error = $"avaible tickets is only {movie.MAvailable} but you have ordered {noTickets}"
            });
        }

        // Mule tiered pricing rule.
        var price = noTickets <= 5
            ? noTickets * 100m
            : noTickets <= 10
                ? noTickets * 90m
                : noTickets * 80m;

        var order = new Order
        {
            MId = m_id,
            NoTickets = noTickets,
            Price = price
        };

        _db.Orders.Add(order);
        movie.MAvailable -= noTickets;
        await _db.SaveChangesAsync();

        return await WriteJsonAsync(req, HttpStatusCode.OK, order);
    }

    private static async Task<HttpResponseData> WriteJsonAsync<T>(HttpRequestData req, HttpStatusCode status, T body)
    {
        var response = req.CreateResponse(status);
        await response.WriteAsJsonAsync(body);
        response.StatusCode = status;
        return response;
    }
}
