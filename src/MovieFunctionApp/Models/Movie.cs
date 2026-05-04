using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MovieFunctionApp.Models;

/// <summary>
/// Movie entity, mapped from the Mule <c>movie_table</c>.
/// </summary>
public class Movie
{
    /// <summary>Movie id (primary key).</summary>
    [Key]
    [Column("m_id")]
    [JsonPropertyName("m_id")]
    public int MId { get; set; }

    /// <summary>Movie name.</summary>
    [Column("m_name")]
    [JsonPropertyName("m_name")]
    public string MName { get; set; } = string.Empty;

    /// <summary>Number of available tickets.</summary>
    [Column("m_available")]
    [JsonPropertyName("m_available")]
    public int MAvailable { get; set; }
}
