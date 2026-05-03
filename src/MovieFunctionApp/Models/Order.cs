using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MovieFunctionApp.Models;

/// <summary>
/// Represents a booking row in the original Mule <c>order_table</c>.
/// </summary>
[Table("order_table")]
public class Order
{
    /// <summary>Primary key (Mule column <c>o_id</c>), auto-incremented.</summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("o_id")]
    [JsonPropertyName("o_id")]
    public int OId { get; set; }

    /// <summary>FK to the booked movie (Mule column <c>m_id</c>).</summary>
    [Column("m_id")]
    [JsonPropertyName("m_id")]
    public int MId { get; set; }

    /// <summary>Number of tickets booked (Mule column <c>no_tickets</c>).</summary>
    [Column("no_tickets")]
    [JsonPropertyName("no_tickets")]
    public int NoTickets { get; set; }

    /// <summary>Total computed price (Mule column <c>price</c>).</summary>
    [Column("price")]
    [JsonPropertyName("price")]
    public int Price { get; set; }
}
