using System.Text.Json.Serialization;

namespace MovieFunctionApp.Data;

/// <summary>
/// Represents a movie in the catalog. Mirrors columns of the Mule
/// <c>movie_table</c> (m_id, m_name, m_available, m_price ...).
/// </summary>
public class Movie
{
    [JsonPropertyName("m_id")]
    public int MId { get; set; }

    [JsonPropertyName("m_name")]
    public string MName { get; set; } = string.Empty;

    [JsonPropertyName("m_available")]
    public int MAvailable { get; set; }

    [JsonPropertyName("m_price")]
    public decimal MPrice { get; set; }
}
