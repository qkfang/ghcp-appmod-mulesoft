using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using MySqlConnector;

namespace BookMyShow.Functions;

public class MoviesFunctions
{
    private readonly ILogger<MoviesFunctions> _logger;

    public MoviesFunctions(ILogger<MoviesFunctions> logger)
    {
        _logger = logger;
    }

    [Function("GetMovies")]
    [OpenApiOperation(operationId: "GetMovies", tags: new[] { "movies" }, Summary = "List available movies", Description = "Returns all movies that have available tickets.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(IEnumerable<Movie>), Summary = "Movies returned successfully")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/json", bodyType: typeof(ErrorResponse), Summary = "Unexpected error")]
    public async Task<IActionResult> GetMovies(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "movies")] HttpRequest req)
    {
        _logger.LogInformation("GetMovies function triggered.");
        try
        {
            await using var conn = Db.CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand(
                "SELECT m_id, m_name, m_available FROM movie_table WHERE m_available > 0", conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            var movies = new List<Movie>();
            while (await reader.ReadAsync())
            {
                movies.Add(new Movie(
                    reader.GetInt32("m_id"),
                    reader.GetString("m_name"),
                    reader.GetInt32("m_available")));
            }
            return new OkObjectResult(movies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetMovies");
            return new ObjectResult(new ErrorResponse(ex.Message)) { StatusCode = 500 };
        }
    }

    [Function("BookTickets")]
    [OpenApiOperation(operationId: "BookTickets", tags: new[] { "movies" }, Summary = "Book tickets for a movie", Description = "Books the requested number of tickets for the movie identified by m_id.")]
    [OpenApiParameter(name: "m_id", In = ParameterLocation.Path, Required = true, Type = typeof(int), Summary = "Movie identifier")]
    [OpenApiParameter(name: "no_tickets", In = ParameterLocation.Query, Required = true, Type = typeof(int), Summary = "Number of tickets to book")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(Order), Summary = "Booking confirmed")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(ErrorResponse), Summary = "Invalid request")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(ErrorResponse), Summary = "Movie not found")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/json", bodyType: typeof(ErrorResponse), Summary = "Unexpected error")]
    public async Task<IActionResult> BookTickets(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "movies/{m_id}")] HttpRequest req,
        string m_id)
    {
        _logger.LogInformation("BookTickets function triggered.");

        var noTicketsStr = req.Query["no_tickets"].ToString();
        if (string.IsNullOrEmpty(m_id) || string.IsNullOrEmpty(noTicketsStr))
        {
            return new BadRequestObjectResult(
                new ErrorResponse("m_id (path) and no_tickets (query param) are required"));
        }

        if (!int.TryParse(m_id, out var movieId) || !int.TryParse(noTicketsStr, out var noTickets))
        {
            return new BadRequestObjectResult(
                new ErrorResponse("m_id and no_tickets must be integers"));
        }

        if (noTickets <= 0)
        {
            return new BadRequestObjectResult(
                new ErrorResponse("no_tickets must be greater than 0"));
        }

        await using var conn = Db.CreateConnection();
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();

        try
        {
            // Lock the row to prevent concurrent overbooking
            int available;
            await using (var selectCmd = new MySqlCommand(
                "SELECT m_available FROM movie_table WHERE m_id = @m_id FOR UPDATE", conn, tx))
            {
                selectCmd.Parameters.AddWithValue("@m_id", movieId);
                var result = await selectCmd.ExecuteScalarAsync();
                if (result is null || result is DBNull)
                {
                    await tx.RollbackAsync();
                    return new NotFoundObjectResult(new ErrorResponse("Movie not found"));
                }
                available = Convert.ToInt32(result);
            }

            if (available - noTickets < 0)
            {
                await tx.RollbackAsync();
                return new BadRequestObjectResult(new ErrorResponse(
                    $"available tickets is only {available} but you have ordered {noTickets}"));
            }

            // Pricing tiers (mirrors original MuleSoft logic)
            var price = noTickets switch
            {
                <= 5 => noTickets * 100,
                <= 10 => noTickets * 90,
                _ => noTickets * 80,
            };

            long newOrderId;
            await using (var insertCmd = new MySqlCommand(
                "INSERT INTO order_table (m_id, no_tickets, price) VALUES (@m_id, @no_tickets, @price)",
                conn, tx))
            {
                insertCmd.Parameters.AddWithValue("@m_id", movieId);
                insertCmd.Parameters.AddWithValue("@no_tickets", noTickets);
                insertCmd.Parameters.AddWithValue("@price", price);
                await insertCmd.ExecuteNonQueryAsync();
                newOrderId = insertCmd.LastInsertedId;
            }

            await using (var updateCmd = new MySqlCommand(
                "UPDATE movie_table SET m_available = m_available - @n WHERE m_id = @m_id", conn, tx))
            {
                updateCmd.Parameters.AddWithValue("@n", noTickets);
                updateCmd.Parameters.AddWithValue("@m_id", movieId);
                await updateCmd.ExecuteNonQueryAsync();
            }

            Order? order = null;
            await using (var fetchCmd = new MySqlCommand(
                "SELECT o_id, m_id, no_tickets, price FROM order_table WHERE o_id = @o_id", conn, tx))
            {
                fetchCmd.Parameters.AddWithValue("@o_id", newOrderId);
                await using var reader = await fetchCmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    order = new Order(
                        reader.GetInt32("o_id"),
                        reader.GetInt32("m_id"),
                        reader.GetInt32("no_tickets"),
                        reader.GetInt32("price"));
                }
            }

            await tx.CommitAsync();
            return new OkObjectResult(order);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            _logger.LogError(ex, "Error in BookTickets");
            return new ObjectResult(new ErrorResponse(ex.Message)) { StatusCode = 500 };
        }
    }
}
