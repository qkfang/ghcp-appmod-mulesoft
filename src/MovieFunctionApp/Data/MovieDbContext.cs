using Microsoft.EntityFrameworkCore;
using MovieFunctionApp.Models;

namespace MovieFunctionApp.Data;

/// <summary>
/// EF Core in-memory database context replacing the original MySQL
/// <c>Database_Config</c> connector (mulesoft/src/main/mule/global.xml).
/// No external database is required.
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
        modelBuilder.Entity<Movie>().HasKey(m => m.MId);

        modelBuilder.Entity<Order>(order =>
        {
            order.HasKey(o => o.OId);
            order.Property(o => o.OId).ValueGeneratedOnAdd();
        });
    }

    /// <summary>
    /// Seeds a handful of movies so <c>GET /api/movies</c> has data to return on first run.
    /// </summary>
    public static void Seed(MovieDbContext db)
    {
        if (db.Movies.Any())
        {
            return;
        }

        db.Movies.AddRange(
            new Movie { MId = 1, MName = "The Shawshank Redemption", MAvailable = 50 },
            new Movie { MId = 2, MName = "Inception", MAvailable = 30 },
            new Movie { MId = 3, MName = "Interstellar", MAvailable = 20 },
            new Movie { MId = 4, MName = "The Dark Knight", MAvailable = 40 },
            new Movie { MId = 5, MName = "Pulp Fiction", MAvailable = 0 });

        db.SaveChanges();
    }
}
