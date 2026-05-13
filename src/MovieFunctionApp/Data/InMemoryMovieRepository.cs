using MovieFunctionApp.Models;

namespace MovieFunctionApp.Data;

/// <summary>
/// In-memory implementation of <see cref="IMovieRepository"/>. Seeded with
/// sample data so the migrated API is usable without provisioning a database.
/// </summary>
public class InMemoryMovieRepository : IMovieRepository
{
    private readonly object _gate = new();
    private readonly List<Movie> _movies;
    private readonly List<Order> _orders = new();
    private int _nextOrderId = 1;

    public InMemoryMovieRepository()
    {
        _movies = new List<Movie>
        {
            new() { MId = 1, MName = "The Matrix",       MLanguage = "English", MAvailable = 25 },
            new() { MId = 2, MName = "Inception",        MLanguage = "English", MAvailable = 12 },
            new() { MId = 3, MName = "Spirited Away",    MLanguage = "Japanese", MAvailable = 8  },
            new() { MId = 4, MName = "RRR",              MLanguage = "Telugu",  MAvailable = 0  },
            new() { MId = 5, MName = "Parasite",         MLanguage = "Korean",  MAvailable = 30 },
        };
    }

    public IReadOnlyList<Movie> GetAvailableMovies()
    {
        lock (_gate)
        {
            return _movies.Where(m => m.MAvailable > 0).ToList();
        }
    }

    public Movie? GetMovieById(int mId)
    {
        lock (_gate)
        {
            return _movies.FirstOrDefault(m => m.MId == mId);
        }
    }

    public Order BookTickets(int mId, int noTickets)
    {
        if (noTickets <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(noTickets), "no_tickets must be a positive integer.");
        }

        lock (_gate)
        {
            var movie = _movies.FirstOrDefault(m => m.MId == mId)
                ?? throw new KeyNotFoundException($"Movie {mId} not found.");

            if (movie.MAvailable - noTickets < 0)
            {
                throw new InvalidOperationException(
                    $"available tickets is only {movie.MAvailable} but you have ordered {noTickets}");
            }

            var price = CalculatePrice(noTickets);

            var order = new Order
            {
                OId = _nextOrderId++,
                MId = mId,
                NoTickets = noTickets,
                Price = price,
            };
            _orders.Add(order);

            movie.MAvailable -= noTickets;
            return order;
        }
    }

    /// <summary>
    /// Pricing rules ported verbatim from the Mule DataWeave expression in
    /// <c>implementation.xml</c>.
    /// </summary>
    private static int CalculatePrice(int noTickets) => noTickets switch
    {
        <= 5  => noTickets * 100,
        <= 10 => noTickets * 90,
        _     => noTickets * 80,
    };
}
