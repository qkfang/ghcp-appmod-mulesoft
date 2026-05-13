namespace MovieFunctionApp.Models;

/// <summary>
/// Represents a movie record. Mirrors the Mule <c>movie_table</c> source.
/// </summary>
public class Movie
{
    /// <summary>Movie identifier.</summary>
    public int MId { get; set; }

    /// <summary>Movie name.</summary>
    public string MName { get; set; } = string.Empty;

    /// <summary>Movie language.</summary>
    public string MLanguage { get; set; } = string.Empty;

    /// <summary>Number of tickets still available.</summary>
    public int MAvailable { get; set; }
}
