using System.Text.Json.Serialization;

namespace MovieFunctionApp.Models;

/// <summary>
/// Represents a row from the original MuleSoft <c>movie_table</c>.
/// Property names mirror the raw SQL column names used in
/// <c>mulesoft/src/main/mule/implementation.xml</c> for JSON parity.
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
