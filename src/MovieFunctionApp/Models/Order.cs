using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MovieFunctionApp.Models;

/// <summary>
/// Represents a booking row, mirroring the Mule <c>order_table</c> schema.
/// </summary>
public class Order
{
    [Key]
    [Column("o_id")]
    [JsonPropertyName("o_id")]
    public int OId { get; set; }

    [Column("m_id")]
    [JsonPropertyName("m_id")]
    public int MId { get; set; }

    [Column("no_tickets")]
    [JsonPropertyName("no_tickets")]
    public int NoTickets { get; set; }

    [Column("price")]
    [JsonPropertyName("price")]
    public int Price { get; set; }
}
