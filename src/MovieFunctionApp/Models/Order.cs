using System.Text.Json.Serialization;

namespace MovieFunctionApp.Models;

/// <summary>
/// A booking order created for a movie.
/// Mirrors the <c>order_table</c> columns written/read by the original Mule
/// <c>BookTickets</c> flow (see mulesoft/src/main/mule/implementation.xml).
/// </summary>
public class Order
{
    [JsonPropertyName("o_id")]
    public int OId { get; set; }

    [JsonPropertyName("m_id")]
    public int MId { get; set; }

    [JsonPropertyName("no_tickets")]
    public int NoTickets { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }
}
