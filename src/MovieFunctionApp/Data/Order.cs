using System.Text.Json.Serialization;

namespace MovieFunctionApp.Data;

/// <summary>
/// Represents an order in the system. Mirrors columns of the Mule
/// <c>order_table</c> (o_id, m_id, no_tickets, price).
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
