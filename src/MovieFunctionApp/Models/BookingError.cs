using System.Text.Json.Serialization;

namespace MovieFunctionApp.Models;

/// <summary>
/// Error payload shape returned when a ticket booking fails validation,
/// matching the <c>on-error-continue</c> handler for
/// <c>VALIDATION:INVALID_BOOLEAN</c> in <c>implementation.xml</c>.
/// </summary>
public class BookingError
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;
}
