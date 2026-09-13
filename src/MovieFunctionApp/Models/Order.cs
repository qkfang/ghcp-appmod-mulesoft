using System.Text.Json.Serialization;

namespace MovieFunctionApp.Models;

/// <summary>
/// Represents a row from the original MuleSoft <c>order_table</c>.
/// Property names mirror the raw SQL column names used in
/// <c>mulesoft/src/main/mule/implementation.xml</c> for JSON parity.
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
    public int Price { get; set; }
}
