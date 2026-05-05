using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MovieFunctionApp.Data;

/// <summary>
/// Represents a movie record. Mirrors the Mule <c>movie_table</c> schema
/// (<c>m_id</c>, <c>m_name</c>, <c>m_available</c>) used by the original
/// MuleSoft application's <c>GetMovies</c> and <c>BookTickets</c> flows.
/// </summary>
public class Movie
{
    [Key]
    [Column("m_id")]
    [JsonPropertyName("m_id")]
    public int MId { get; set; }

    [Column("m_name")]
    [JsonPropertyName("m_name")]
    public string MName { get; set; } = string.Empty;

    [Column("m_available")]
    [JsonPropertyName("m_available")]
    public int MAvailable { get; set; }
}
