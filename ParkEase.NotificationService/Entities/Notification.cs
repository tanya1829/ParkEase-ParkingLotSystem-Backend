namespace ParkEase.NotificationService.Entities;

/// <summary>
/// Represents a notification sent to a user.
/// Tracks read/unread state and supports multiple channels.
/// </summary>
public class Notification
{
    public int NotificationId { get; set; }
    public int RecipientId { get; set; }              // UserId from Auth Service
    public string Type { get; set; } = string.Empty;  // BOOKING | CHECKIN | CHECKOUT | PAYMENT | EXPIRY | PROMO
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Channel { get; set; } = "APP";      // APP | EMAIL | SMS
    public int? RelatedId { get; set; }               // BookingId or PaymentId
    public string? RelatedType { get; set; }          // BOOKING | PAYMENT
    public bool IsRead { get; set; } = false;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
