namespace ParkEase.PaymentService.Entities;

/// <summary>
/// Represents a payment transaction linked to a booking.
/// Each booking has exactly one payment record.
/// </summary>
public class Payment
{
    public int PaymentId { get; set; }
    public int BookingId { get; set; }
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "PENDING";     // PENDING | PAID | REFUNDED | FAILED
    public string Mode { get; set; } = "CASH";          // CARD | UPI | WALLET | CASH
    public string? TransactionId { get; set; }          // from payment gateway
    public string Currency { get; set; } = "INR";
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
    public DateTime? RefundedAt { get; set; }
}
