namespace MovieFunctionApp.Models;

/// <summary>
/// Represents an order record. Mirrors the Mule <c>order_table</c> source.
/// </summary>
public class Order
{
    /// <summary>Order identifier (auto-incremented).</summary>
    public int OId { get; set; }

    /// <summary>Identifier of the booked movie.</summary>
    public int MId { get; set; }

    /// <summary>Number of tickets booked.</summary>
    public int NoTickets { get; set; }

    /// <summary>Calculated price for the booking.</summary>
    public int Price { get; set; }
}
