using System.Text.Json.Serialization;

namespace MovieFunctionApp.Models;

/// <summary>
/// A movie available for booking.
/// Mirrors the <c>movie_table</c> columns queried by the original Mule
/// <c>GetMovies</c>/<c>BookTickets</c> flows (see mulesoft/src/main/mule/implementation.xml).
/// </summary>
public class Movie
{
    [JsonPropertyName("m_id")]
    public int MId { get; set; }

    [JsonPropertyName("m_name")]
    public string MName { get; set; } = string.Empty;

    [JsonPropertyName("m_available")]
    public int MAvailable { get; set; }
}
