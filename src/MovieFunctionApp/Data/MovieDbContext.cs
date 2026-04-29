using Microsoft.EntityFrameworkCore;
using MovieFunctionApp.Models;

namespace MovieFunctionApp.Data;

public class MovieDbContext : DbContext
{
    public MovieDbContext(DbContextOptions<MovieDbContext> options) : base(options)
    {
    }

    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Movie>().HasKey(m => m.M_Id);
        modelBuilder.Entity<Order>(e =>
        {
            e.HasKey(o => o.O_Id);
            e.Property(o => o.O_Id).ValueGeneratedOnAdd();
        });
    }

    public static void Seed(MovieDbContext db)
    {
        if (db.Movies.Any()) return;

        db.Movies.AddRange(
            new Movie { M_Id = 1, M_Name = "The Shawshank Redemption", M_Available = 50 },
            new Movie { M_Id = 2, M_Name = "Inception", M_Available = 30 },
            new Movie { M_Id = 3, M_Name = "Interstellar", M_Available = 20 },
            new Movie { M_Id = 4, M_Name = "The Dark Knight", M_Available = 40 },
            new Movie { M_Id = 5, M_Name = "Pulp Fiction", M_Available = 0 }
        );
        db.SaveChanges();
    }
}
