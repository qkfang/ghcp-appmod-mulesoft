using Microsoft.EntityFrameworkCore;
using MovieFunctionApp.Models;

namespace MovieFunctionApp.Data;

/// <summary>
/// EF Core <see cref="DbContext"/> backing the migrated Mule database.
/// Currently configured against an in-memory provider; production deployments
/// should swap the provider via <c>SqlConnectionString</c> configuration.
/// </summary>
public class MovieDbContext : DbContext
{
    public MovieDbContext(DbContextOptions<MovieDbContext> options) : base(options)
    {
    }

    /// <summary>Movies catalog (replaces <c>movie_table</c>).</summary>
    public DbSet<Movie> Movies => Set<Movie>();

    /// <summary>Bookings ledger (replaces <c>order_table</c>).</summary>
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Seed sample movies so the API is usable immediately against the in-memory DB.
        modelBuilder.Entity<Movie>().HasData(
            new Movie { MId = 1, MName = "The Matrix", MAvailable = 50 },
            new Movie { MId = 2, MName = "Inception", MAvailable = 30 },
            new Movie { MId = 3, MName = "Interstellar", MAvailable = 0 },
            new Movie { MId = 4, MName = "Dune", MAvailable = 20 }
        );
    }
}
