using System.Text.Json.Serialization;

namespace MovieFunctionApp.Models;

/// <summary>
/// Error payload returned when a booking cannot be fulfilled,
/// mirroring the Mule <c>on-error-continue</c> response shape.
/// </summary>
public class BookingError
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;
}
