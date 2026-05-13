using MovieFunctionApp.Models;

namespace MovieFunctionApp.Data;

/// <summary>
/// Abstraction over the movie / order persistence layer. Replaces the Mule
/// <c>Database_Config</c> connector with a pluggable repository.
/// </summary>
public interface IMovieRepository
{
    /// <summary>
    /// Returns every movie with one or more tickets still available
    /// (equivalent to <c>select * from movie_table where m_available &gt; 0</c>).
    /// </summary>
    IReadOnlyList<Movie> GetAvailableMovies();

    /// <summary>
    /// Looks up a single movie by identifier.
    /// </summary>
    Movie? GetMovieById(int mId);

    /// <summary>
    /// Books <paramref name="noTickets"/> seats for the supplied movie. Throws
    /// <see cref="InvalidOperationException"/> when capacity is insufficient
    /// (mirrors the Mule <c>VALIDATION:INVALID_BOOLEAN</c> branch).
    /// </summary>
    Order BookTickets(int mId, int noTickets);
}
