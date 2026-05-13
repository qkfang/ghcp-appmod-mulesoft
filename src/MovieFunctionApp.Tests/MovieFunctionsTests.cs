using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MovieFunctionApp.Data;
using MovieFunctionApp.Functions;

namespace MovieFunctionApp.Tests;

/// <summary>
/// Unit tests for <see cref="MovieFunctions"/> covering all HTTP trigger behaviours
/// and the pricing helper. A fresh in-memory <see cref="MovieDbContext"/> is created
/// per test so tests are fully isolated.
/// </summary>
public class MovieFunctionsTests
{
    // ─── helpers ─────────────────────────────────────────────────────────────

    private static MovieDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<MovieDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new MovieDbContext(options);
    }

    private static HttpRequest CreateHttpRequest(string? noTickets = null)
    {
        var ctx = new DefaultHttpContext();
        if (noTickets is not null)
            ctx.Request.QueryString = new QueryString($"?no_tickets={noTickets}");
        return ctx.Request;
    }

    // ─── CalculatePrice ───────────────────────────────────────────────────────

    [Theory]
    [InlineData(1, 100)]   // tier 1: 1×100
    [InlineData(5, 500)]   // tier 1 boundary: 5×100
    [InlineData(6, 540)]   // tier 2: 6×90
    [InlineData(10, 900)]  // tier 2 boundary: 10×90
    [InlineData(11, 880)]  // tier 3: 11×80
    [InlineData(20, 1600)] // tier 3: 20×80
    public void CalculatePrice_ReturnsCorrectTieredPrice(int tickets, int expected)
    {
        Assert.Equal(expected, MovieFunctions.CalculatePrice(tickets));
    }

    // ─── GetMovies ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMovies_ReturnsOnlyMoviesWithAvailabilityGreaterThanZero()
    {
        using var db = CreateDbContext(nameof(GetMovies_ReturnsOnlyMoviesWithAvailabilityGreaterThanZero));
        db.Movies.AddRange(
            new Movie { MId = 1, MName = "Available", MAvailable = 5 },
            new Movie { MId = 2, MName = "Sold Out", MAvailable = 0 });
        db.SaveChanges();

        var sut = new MovieFunctions(NullLogger<MovieFunctions>.Instance, db);
        var result = await sut.GetMovies(CreateHttpRequest()) as OkObjectResult;

        Assert.NotNull(result);
        var movies = Assert.IsAssignableFrom<IEnumerable<Movie>>(result.Value).ToList();
        Assert.Single(movies);
        Assert.Equal(1, movies[0].MId);
    }

    [Fact]
    public async Task GetMovies_WhenAllMoviesUnavailable_ReturnsEmptyList()
    {
        using var db = CreateDbContext(nameof(GetMovies_WhenAllMoviesUnavailable_ReturnsEmptyList));
        db.Movies.Add(new Movie { MId = 1, MName = "Sold Out", MAvailable = 0 });
        db.SaveChanges();

        var sut = new MovieFunctions(NullLogger<MovieFunctions>.Instance, db);
        var result = await sut.GetMovies(CreateHttpRequest()) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Empty(Assert.IsAssignableFrom<IEnumerable<Movie>>(result.Value));
    }

    [Fact]
    public async Task GetMovies_EmptyDatabase_ReturnsEmptyList()
    {
        using var db = CreateDbContext(nameof(GetMovies_EmptyDatabase_ReturnsEmptyList));

        var sut = new MovieFunctions(NullLogger<MovieFunctions>.Instance, db);
        var result = await sut.GetMovies(CreateHttpRequest()) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Empty(Assert.IsAssignableFrom<IEnumerable<Movie>>(result.Value));
    }

    [Fact]
    public async Task GetMovies_ReturnsAllAvailableMovies()
    {
        using var db = CreateDbContext(nameof(GetMovies_ReturnsAllAvailableMovies));
        db.Movies.AddRange(
            new Movie { MId = 1, MName = "Alpha", MAvailable = 10 },
            new Movie { MId = 2, MName = "Beta", MAvailable = 1 },
            new Movie { MId = 3, MName = "Sold Out", MAvailable = 0 });
        db.SaveChanges();

        var sut = new MovieFunctions(NullLogger<MovieFunctions>.Instance, db);
        var result = await sut.GetMovies(CreateHttpRequest()) as OkObjectResult;

        Assert.NotNull(result);
        var movies = Assert.IsAssignableFrom<IEnumerable<Movie>>(result.Value).ToList();
        Assert.Equal(2, movies.Count);
        Assert.DoesNotContain(movies, m => m.MAvailable == 0);
    }

    // ─── BookTickets – input validation ──────────────────────────────────────

    [Fact]
    public async Task BookTickets_NonNumericMovieId_ReturnsBadRequest()
    {
        using var db = CreateDbContext(nameof(BookTickets_NonNumericMovieId_ReturnsBadRequest));
        var sut = new MovieFunctions(NullLogger<MovieFunctions>.Instance, db);

        var result = await sut.BookTickets(CreateHttpRequest("2"), "abc");

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task BookTickets_MissingNoTicketsQueryParam_ReturnsBadRequest()
    {
        using var db = CreateDbContext(nameof(BookTickets_MissingNoTicketsQueryParam_ReturnsBadRequest));
        var sut = new MovieFunctions(NullLogger<MovieFunctions>.Instance, db);

        var result = await sut.BookTickets(CreateHttpRequest(), "1");

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task BookTickets_ZeroNoTickets_ReturnsBadRequest()
    {
        using var db = CreateDbContext(nameof(BookTickets_ZeroNoTickets_ReturnsBadRequest));
        var sut = new MovieFunctions(NullLogger<MovieFunctions>.Instance, db);

        var result = await sut.BookTickets(CreateHttpRequest("0"), "1");

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task BookTickets_NegativeNoTickets_ReturnsBadRequest()
    {
        using var db = CreateDbContext(nameof(BookTickets_NegativeNoTickets_ReturnsBadRequest));
        var sut = new MovieFunctions(NullLogger<MovieFunctions>.Instance, db);

        var result = await sut.BookTickets(CreateHttpRequest("-3"), "1");

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task BookTickets_NonNumericNoTickets_ReturnsBadRequest()
    {
        using var db = CreateDbContext(nameof(BookTickets_NonNumericNoTickets_ReturnsBadRequest));
        var sut = new MovieFunctions(NullLogger<MovieFunctions>.Instance, db);

        var result = await sut.BookTickets(CreateHttpRequest("abc"), "1");

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ─── BookTickets – movie lookup ───────────────────────────────────────────

    [Fact]
    public async Task BookTickets_MovieNotFound_ReturnsNotFound()
    {
        using var db = CreateDbContext(nameof(BookTickets_MovieNotFound_ReturnsNotFound));
        var sut = new MovieFunctions(NullLogger<MovieFunctions>.Instance, db);

        var result = await sut.BookTickets(CreateHttpRequest("2"), "999") as NotFoundObjectResult;

        Assert.NotNull(result);
        var body = Assert.IsType<MessageResponse>(result.Value);
        Assert.Equal("Resource not found", body.Message);
    }

    // ─── BookTickets – availability check ────────────────────────────────────

    [Fact]
    public async Task BookTickets_InsufficientAvailability_ReturnsBadRequestWithMuleErrorShape()
    {
        using var db = CreateDbContext(nameof(BookTickets_InsufficientAvailability_ReturnsBadRequestWithMuleErrorShape));
        db.Movies.Add(new Movie { MId = 1, MName = "Scarce", MAvailable = 2 });
        db.SaveChanges();

        var sut = new MovieFunctions(NullLogger<MovieFunctions>.Instance, db);
        var result = await sut.BookTickets(CreateHttpRequest("5"), "1") as BadRequestObjectResult;

        Assert.NotNull(result);
        var body = Assert.IsType<BookingError>(result.Value);
        // The misspelling "avaible" is preserved verbatim from the Mule implementation.xml
        Assert.Equal("avaible tickets is only 2 but you have ordered 5", body.Error);
    }

    [Fact]
    public async Task BookTickets_ExactAvailabilityRequested_Succeeds()
    {
        using var db = CreateDbContext(nameof(BookTickets_ExactAvailabilityRequested_Succeeds));
        db.Movies.Add(new Movie { MId = 1, MName = "Last Chance", MAvailable = 5 });
        db.SaveChanges();

        var sut = new MovieFunctions(NullLogger<MovieFunctions>.Instance, db);
        var result = await sut.BookTickets(CreateHttpRequest("5"), "1");

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(0, db.Movies.Find(1)!.MAvailable);
    }

    // ─── BookTickets – happy path ─────────────────────────────────────────────

    [Fact]
    public async Task BookTickets_ValidRequest_ReturnsCreatedOrderWithCorrectFields()
    {
        using var db = CreateDbContext(nameof(BookTickets_ValidRequest_ReturnsCreatedOrderWithCorrectFields));
        db.Movies.Add(new Movie { MId = 42, MName = "Test Movie", MAvailable = 20 });
        db.SaveChanges();

        var sut = new MovieFunctions(NullLogger<MovieFunctions>.Instance, db);
        var result = await sut.BookTickets(CreateHttpRequest("3"), "42") as OkObjectResult;

        Assert.NotNull(result);
        var order = Assert.IsType<Order>(result.Value);
        Assert.Equal(42, order.MId);
        Assert.Equal(3, order.NoTickets);
        Assert.Equal(300, order.Price); // 3 × 100 (tier 1)
    }

    [Fact]
    public async Task BookTickets_ValidRequest_DecrementsMovieAvailability()
    {
        using var db = CreateDbContext(nameof(BookTickets_ValidRequest_DecrementsMovieAvailability));
        db.Movies.Add(new Movie { MId = 1, MName = "Blockbuster", MAvailable = 10 });
        db.SaveChanges();

        var sut = new MovieFunctions(NullLogger<MovieFunctions>.Instance, db);
        await sut.BookTickets(CreateHttpRequest("4"), "1");

        Assert.Equal(6, db.Movies.Find(1)!.MAvailable);
    }

    [Fact]
    public async Task BookTickets_ValidRequest_PersistsOrderInDatabase()
    {
        using var db = CreateDbContext(nameof(BookTickets_ValidRequest_PersistsOrderInDatabase));
        db.Movies.Add(new Movie { MId = 1, MName = "Blockbuster", MAvailable = 10 });
        db.SaveChanges();

        var sut = new MovieFunctions(NullLogger<MovieFunctions>.Instance, db);
        await sut.BookTickets(CreateHttpRequest("2"), "1");

        Assert.Single(db.Orders);
        var saved = db.Orders.First();
        Assert.Equal(1, saved.MId);
        Assert.Equal(2, saved.NoTickets);
    }

    // ─── BookTickets – pricing tiers ─────────────────────────────────────────

    [Theory]
    [InlineData(5, 500)]   // tier 1 upper boundary: 5×100
    [InlineData(6, 540)]   // tier 2 lower boundary: 6×90
    [InlineData(10, 900)]  // tier 2 upper boundary: 10×90
    [InlineData(11, 880)]  // tier 3 lower boundary: 11×80
    [InlineData(15, 1200)] // tier 3 mid: 15×80
    public async Task BookTickets_PricingTiers_CorrectPrice(int noTickets, int expectedPrice)
    {
        var dbName = $"Pricing_{noTickets}_{nameof(BookTickets_PricingTiers_CorrectPrice)}";
        using var db = CreateDbContext(dbName);
        db.Movies.Add(new Movie { MId = 1, MName = "Test", MAvailable = 50 });
        db.SaveChanges();

        var sut = new MovieFunctions(NullLogger<MovieFunctions>.Instance, db);
        var result = await sut.BookTickets(CreateHttpRequest(noTickets.ToString()), "1") as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(expectedPrice, Assert.IsType<Order>(result.Value).Price);
    }
}
