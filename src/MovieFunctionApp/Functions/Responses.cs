using System.Text.Json.Serialization;

namespace MovieFunctionApp.Functions;

/// <summary>
/// Generic JSON message envelope used for the APIKIT error responses
/// (Bad request / Resource not found / Method not allowed / etc.) defined
/// in the Mule <c>movie-main</c> flow.
/// </summary>
public class MessageResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Error payload returned by <c>BookTickets</c> when the requested ticket
/// count exceeds availability. Matches the Mule
/// <c>VALIDATION:INVALID_BOOLEAN</c> on-error-continue payload shape.
/// </summary>
public class BookingError
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;
}
