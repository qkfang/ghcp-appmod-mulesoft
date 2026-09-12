using System.Text.Json.Serialization;

namespace MovieFunctionApp.Models;

/// <summary>
/// Generic <c>{"message": "..."}</c> response used for malformed-request errors,
/// mirroring the APIKit error handlers (bad request/resource not found) declared in
/// mulesoft/src/main/mule/interface.xml.
/// </summary>
public class ApiMessage
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
