using System.Text.Json.Serialization;

namespace MovieFunctionApp.Models;

/// <summary>
/// Error payload returned by the booking endpoint, matching the Mule
/// <c>{ "error": "..." }</c> shape from the
/// <c>VALIDATION:INVALID_BOOLEAN</c> handler in the original BookTickets flow.
/// </summary>
public class BookingError
{
    /// <summary>Human-readable error message.</summary>
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;
}
