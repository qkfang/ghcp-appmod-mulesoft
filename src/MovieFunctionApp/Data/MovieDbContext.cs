using Microsoft.EntityFrameworkCore;

namespace MovieFunctionApp.Data;

/// <summary>
/// EF Core <see cref="DbContext"/> backing the in-memory store used in place
/// of the MuleSoft application's MySQL <c>Database_Config</c>. The schema
/// (<c>movie_table</c>, <c>order_table</c>) is preserved for parity with the
/// original Mule flows.
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
        // Table names from the original Mule schema are kept here as
        // metadata via Column attributes on the entities; the in-memory
        // provider does not require relational ToTable() mappings.
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Seeds the in-memory store with sample movies so that
    /// <c>GET /api/movies</c> returns data on a fresh start.
    /// </summary>
    public static void Seed(MovieDbContext db)
    {
        if (db.Movies.Any())
        {
            return;
        }

        db.Movies.AddRange(
            new Movie { MId = 1, MName = "The Matrix", MAvailable = 50 },
            new Movie { MId = 2, MName = "Inception", MAvailable = 30 },
            new Movie { MId = 3, MName = "Interstellar", MAvailable = 20 },
            new Movie { MId = 4, MName = "The Dark Knight", MAvailable = 0 },
            new Movie { MId = 5, MName = "Dune", MAvailable = 100 }
        );
        db.SaveChanges();
    }
}
