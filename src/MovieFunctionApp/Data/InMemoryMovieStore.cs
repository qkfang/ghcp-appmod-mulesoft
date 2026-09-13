using MovieFunctionApp.Models;

namespace MovieFunctionApp.Data;

/// <summary>
/// Thread-safe in-memory substitute for the MySQL <c>movie_table</c> /
/// <c>order_table</c> pair used by the original MuleSoft app
/// (<c>mulesoft/src/main/mule/global.xml</c> <c>Database_Config</c>).
/// No external database is required; state resets on process restart.
/// </summary>
public class InMemoryMovieStore
{
    private readonly object _lock = new();
    private readonly List<Movie> _movies;
    private readonly List<Order> _orders = new();
    private int _nextOrderId = 1;

    public InMemoryMovieStore()
    {
        _movies = new List<Movie>
        {
            new() { MId = 1, MName = "The Matrix", MAvailable = 25 },
            new() { MId = 2, MName = "Inception", MAvailable = 10 },
            new() { MId = 3, MName = "Interstellar", MAvailable = 0 },
        };
    }

    /// <summary>Mirrors <c>select * from movie_table where m_available > 0</c>.</summary>
    public IReadOnlyList<Movie> GetAvailableMovies()
    {
        lock (_lock)
        {
            return _movies.Where(m => m.MAvailable > 0).ToList();
        }
    }

    public Movie? GetMovieById(int mId)
    {
        lock (_lock)
        {
            return _movies.FirstOrDefault(m => m.MId == mId);
        }
    }

    /// <summary>
    /// Books tickets for a movie, mirroring the insert/update/select sequence
    /// in the <c>BookTickets</c> flow. Returns <c>null</c> when there is
    /// insufficient availability, leaving the caller to build the parity
    /// error message from <paramref name="movie"/>.
    /// </summary>
    public Order? BookTickets(Movie movie, int noTickets)
    {
        lock (_lock)
        {
            if (movie.MAvailable - noTickets < 0)
            {
                return null;
            }

            var price = noTickets switch
            {
                <= 5 => noTickets * 100,
                <= 10 => noTickets * 90,
                _ => noTickets * 80,
            };

            var order = new Order
            {
                OId = _nextOrderId++,
                MId = movie.MId,
                NoTickets = noTickets,
                Price = price,
            };

            _orders.Add(order);
            movie.MAvailable -= noTickets;

            return order;
        }
    }
}
