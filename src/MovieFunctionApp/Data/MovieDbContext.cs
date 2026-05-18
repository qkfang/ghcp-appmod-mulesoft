using Microsoft.EntityFrameworkCore;

namespace MovieFunctionApp.Data;

/// <summary>
/// EF Core DbContext backing the in-memory movie database.
/// Seeded with sample data to mirror the Mule <c>movie_table</c>.
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
        modelBuilder.Entity<Movie>(b =>
        {
            b.HasKey(m => m.MId);
            b.Property(m => m.MId).ValueGeneratedNever();
        });

        modelBuilder.Entity<Order>(b =>
        {
            b.HasKey(o => o.OId);
            b.Property(o => o.OId).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<Movie>().HasData(
            new Movie { MId = 1, MName = "The Matrix Resurrections", MAvailable = 20, MPrice = 100m },
            new Movie { MId = 2, MName = "Dune", MAvailable = 15, MPrice = 100m },
            new Movie { MId = 3, MName = "Spider-Man: No Way Home", MAvailable = 0, MPrice = 100m },
            new Movie { MId = 4, MName = "Interstellar", MAvailable = 8, MPrice = 100m }
        );
    }
}
