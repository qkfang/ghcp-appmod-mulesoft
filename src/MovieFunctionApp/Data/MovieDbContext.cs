using Microsoft.EntityFrameworkCore;
using MovieFunctionApp.Models;

namespace MovieFunctionApp.Data;

/// <summary>
/// EF Core context for the in-memory movie booking database. Replaces the
/// MuleSoft <c>Database_Config</c> MySQL connection.
/// </summary>
public class MovieDbContext : DbContext
{
    public MovieDbContext(DbContextOptions<MovieDbContext> options) : base(options)
    {
    }

    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Movie>().HasData(
            new Movie { MId = 1, MName = "The Matrix", MAvailable = 50 },
            new Movie { MId = 2, MName = "Inception", MAvailable = 100 },
            new Movie { MId = 3, MName = "Interstellar", MAvailable = 25 },
            new Movie { MId = 4, MName = "Sold Out Movie", MAvailable = 0 });
    }
}
