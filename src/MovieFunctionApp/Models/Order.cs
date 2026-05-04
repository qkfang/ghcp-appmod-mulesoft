using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MovieFunctionApp.Models;

/// <summary>
/// Order entity, mapped from the Mule <c>order_table</c>.
/// </summary>
public class Order
{
    /// <summary>Order id (auto-incremented primary key).</summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("o_id")]
    [JsonPropertyName("o_id")]
    public int OId { get; set; }

    /// <summary>Movie id this order is for.</summary>
    [Column("m_id")]
    [JsonPropertyName("m_id")]
    public int MId { get; set; }

    /// <summary>Number of tickets booked.</summary>
    [Column("no_tickets")]
    [JsonPropertyName("no_tickets")]
    public int NoTickets { get; set; }

    /// <summary>Total price for the booking.</summary>
    [Column("price")]
    [JsonPropertyName("price")]
    public int Price { get; set; }
}
