using System.Text.Json.Serialization;

namespace MovieFunctionApp.Models;

/// <summary>
/// Business-validation error returned by <c>BookTickets</c> when there aren't enough
/// seats available. Mirrors the <c>{"error": "..."}</c> payload built by the
/// <c>VALIDATION:INVALID_BOOLEAN</c> error handler in
/// mulesoft/src/main/mule/implementation.xml.
/// </summary>
public class BookingError
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;
}
