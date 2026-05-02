using Microsoft.EntityFrameworkCore;
using MovieFunctionApp.Models;

namespace MovieFunctionApp.Data;

/// <summary>
/// EF Core context backing the migrated Mule <c>movie_table</c> and <c>order_table</c>.
/// Uses an in-memory provider per the migration issue (no real DB required).
/// </summary>
public class MovieDbContext : DbContext
{
    public MovieDbContext(DbContextOptions<MovieDbContext> options) : base(options)
    {
    }

    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<Order> Orders => Set<Order>();

    /// <summary>
    /// Seeds a few rows so the in-memory store has something to query immediately,
    /// mirroring the seed-style behavior of the original MuleSoft demo.
    /// </summary>
    public static void Seed(MovieDbContext db)
    {
        if (db.Movies.Any())
        {
            return;
        }

        db.Movies.AddRange(
            new Movie { MId = 1, MName = "The Great Adventure", MAvailable = 50 },
            new Movie { MId = 2, MName = "Mystery in the Park", MAvailable = 25 },
            new Movie { MId = 3, MName = "Comedy Hour", MAvailable = 0 },
            new Movie { MId = 4, MName = "Sci-Fi Odyssey", MAvailable = 100 });
        db.SaveChanges();
    }
}
