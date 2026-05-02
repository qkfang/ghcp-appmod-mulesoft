using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MovieFunctionApp.Models;

/// <summary>
/// Represents a movie row in the original Mule <c>movie_table</c>.
/// </summary>
[Table("movie_table")]
public class Movie
{
    /// <summary>Primary key (Mule column <c>m_id</c>).</summary>
    [Key]
    [Column("m_id")]
    [JsonPropertyName("m_id")]
    public int MId { get; set; }

    /// <summary>Movie title (Mule column <c>m_name</c>).</summary>
    [Column("m_name")]
    [JsonPropertyName("m_name")]
    public string? MName { get; set; }

    /// <summary>Number of remaining seats (Mule column <c>m_available</c>).</summary>
    [Column("m_available")]
    [JsonPropertyName("m_available")]
    public int MAvailable { get; set; }
}
