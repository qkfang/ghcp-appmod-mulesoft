using Microsoft.EntityFrameworkCore;
using MovieFunctionApp.Data;

namespace MovieFunctionApp.Tests;

/// <summary>
/// Unit tests for <see cref="MovieDbContext"/> seed logic and entity shape.
/// </summary>
public class MovieDbContextTests
{
    private static MovieDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<MovieDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new MovieDbContext(options);
    }

    [Fact]
    public void Seed_PopulatesMoviesTable()
    {
        using var db = CreateDbContext(nameof(Seed_PopulatesMoviesTable));

        MovieDbContext.Seed(db);

        Assert.True(db.Movies.Any());
    }

    [Fact]
    public void Seed_IsIdempotent_DoesNotDuplicateMovies()
    {
        using var db = CreateDbContext(nameof(Seed_IsIdempotent_DoesNotDuplicateMovies));

        MovieDbContext.Seed(db);
        var countAfterFirst = db.Movies.Count();

        MovieDbContext.Seed(db); // second call should be a no-op
        var countAfterSecond = db.Movies.Count();

        Assert.Equal(countAfterFirst, countAfterSecond);
    }

    [Fact]
    public void Seed_IncludesAtLeastOneUnavailableMovie()
    {
        // Verifies the seed data contains a "sold out" movie so GetMovies
        // filtering can be meaningfully tested against seeded data.
        using var db = CreateDbContext(nameof(Seed_IncludesAtLeastOneUnavailableMovie));

        MovieDbContext.Seed(db);

        Assert.Contains(db.Movies.ToList(), m => m.MAvailable == 0);
    }

    [Fact]
    public void Seed_IncludesAtLeastOneAvailableMovie()
    {
        using var db = CreateDbContext(nameof(Seed_IncludesAtLeastOneAvailableMovie));

        MovieDbContext.Seed(db);

        Assert.Contains(db.Movies.ToList(), m => m.MAvailable > 0);
    }

    [Fact]
    public void Movie_Properties_SetAndGetCorrectly()
    {
        var movie = new Movie { MId = 7, MName = "Test", MAvailable = 42 };

        Assert.Equal(7, movie.MId);
        Assert.Equal("Test", movie.MName);
        Assert.Equal(42, movie.MAvailable);
    }

    [Fact]
    public void Order_Properties_SetAndGetCorrectly()
    {
        var order = new Order { OId = 1, MId = 2, NoTickets = 3, Price = 300 };

        Assert.Equal(1, order.OId);
        Assert.Equal(2, order.MId);
        Assert.Equal(3, order.NoTickets);
        Assert.Equal(300, order.Price);
    }
}
