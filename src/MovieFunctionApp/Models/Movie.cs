using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MovieFunctionApp.Models;

/// <summary>
/// Represents a movie row, mirroring the Mule <c>movie_table</c> schema.
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
