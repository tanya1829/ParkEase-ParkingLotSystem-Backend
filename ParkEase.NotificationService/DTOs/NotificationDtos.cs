namespace ParkEase.NotificationService.DTOs;

// ---------- Request DTOs ----------

/// <summary>Request to send a single notification</summary>
public class SendNotificationRequest
{
    public int RecipientId { get; set; }
    public string Type { get; set; } = string.Empty;    // BOOKING | CHECKIN | CHECKOUT | PAYMENT | EXPIRY | PROMO
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Channel { get; set; } = "APP";        // APP | EMAIL | SMS
    public int? RelatedId { get; set; }
    public string? RelatedType { get; set; }
}

/// <summary>Request to send bulk notifications to multiple users</summary>
public class SendBulkNotificationRequest
{
    public List<int> RecipientIds { get; set; } = new();
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Channel { get; set; } = "APP";
}

// ---------- Response DTOs ----------

/// <summary>Notification details returned in API responses</summary>
public class NotificationDto
{
    public int NotificationId { get; set; }
    public int RecipientId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public int? RelatedId { get; set; }
    public string? RelatedType { get; set; }
    public bool IsRead { get; set; }
    public DateTime SentAt { get; set; }
}

/// <summary>Generic API response wrapper</summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Success") =>
        new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message) =>
        new() { Success = false, Message = message };
}
