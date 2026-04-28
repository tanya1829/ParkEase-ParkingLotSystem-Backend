using ParkEase.NotificationService.DTOs;
using ParkEase.NotificationService.Entities;
using ParkEase.NotificationService.Interfaces;

namespace ParkEase.NotificationService.Services;

/// <summary>
/// Handles in-app notifications for all key ParkEase events.
/// Supports APP, EMAIL, and SMS channels.
/// Email and SMS are stubbed for sprint — ready for real integration later.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repo;
    private readonly ILogger<NotificationService> _logger;

    private static readonly string[] ValidTypes =
        { "BOOKING", "CHECKIN", "CHECKOUT", "PAYMENT", "EXPIRY", "PROMO" };

    private static readonly string[] ValidChannels = { "APP", "EMAIL", "SMS" };

    public NotificationService(INotificationRepository repo, ILogger<NotificationService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<ApiResponse<NotificationDto>> SendAsync(SendNotificationRequest request)
    {
        // Validate type
        if (!ValidTypes.Contains(request.Type.ToUpper()))
            return ApiResponse<NotificationDto>.Fail(
                $"Invalid notification type. Valid: {string.Join(", ", ValidTypes)}");

        // Validate channel
        if (!ValidChannels.Contains(request.Channel.ToUpper()))
            return ApiResponse<NotificationDto>.Fail(
                $"Invalid channel. Valid: {string.Join(", ", ValidChannels)}");

        var notification = new Notification
        {
            RecipientId = request.RecipientId,
            Type = request.Type.ToUpper(),
            Title = request.Title,
            Message = request.Message,
            Channel = request.Channel.ToUpper(),
            RelatedId = request.RelatedId,
            RelatedType = request.RelatedType,
            IsRead = false,
            SentAt = DateTime.UtcNow
        };

        var created = await _repo.CreateAsync(notification);

        // Dispatch to channel
        await DispatchToChannelAsync(notification);

        return ApiResponse<NotificationDto>.Ok(MapToDto(created), "Notification sent successfully.");
    }

    public async Task<ApiResponse<List<NotificationDto>>> SendBulkAsync(SendBulkNotificationRequest request)
    {
        if (!request.RecipientIds.Any())
            return ApiResponse<List<NotificationDto>>.Fail("RecipientIds list cannot be empty.");

        var notifications = request.RecipientIds.Select(id => new Notification
        {
            RecipientId = id,
            Type = request.Type.ToUpper(),
            Title = request.Title,
            Message = request.Message,
            Channel = request.Channel.ToUpper(),
            IsRead = false,
            SentAt = DateTime.UtcNow
        }).ToList();

        var created = await _repo.CreateBulkAsync(notifications);

        return ApiResponse<List<NotificationDto>>.Ok(
            created.Select(MapToDto).ToList(),
            $"{created.Count} notifications sent.");
    }

    public async Task<ApiResponse<List<NotificationDto>>> GetByRecipientAsync(int recipientId)
    {
        var notifications = await _repo.FindByRecipientIdAsync(recipientId);
        return ApiResponse<List<NotificationDto>>.Ok(notifications.Select(MapToDto).ToList());
    }

    public async Task<ApiResponse<NotificationDto>> MarkAsReadAsync(int notificationId)
    {
        var notification = await _repo.FindByNotificationIdAsync(notificationId);
        if (notification == null) return ApiResponse<NotificationDto>.Fail("Notification not found.");

        notification.IsRead = true;
        var updated = await _repo.UpdateAsync(notification);
        return ApiResponse<NotificationDto>.Ok(MapToDto(updated), "Marked as read.");
    }

    public async Task<ApiResponse<string>> MarkAllReadAsync(int recipientId)
    {
        await _repo.MarkAllReadByRecipientIdAsync(recipientId);
        return ApiResponse<string>.Ok("All notifications marked as read.");
    }

    public async Task<ApiResponse<int>> GetUnreadCountAsync(int recipientId)
    {
        var count = await _repo.CountByRecipientIdAndIsReadAsync(recipientId, false);
        return ApiResponse<int>.Ok(count, $"{count} unread notifications.");
    }

    public async Task<ApiResponse<string>> DeleteNotificationAsync(int notificationId)
    {
        var notification = await _repo.FindByNotificationIdAsync(notificationId);
        if (notification == null) return ApiResponse<string>.Fail("Notification not found.");

        await _repo.DeleteByNotificationIdAsync(notificationId);
        return ApiResponse<string>.Ok("Notification deleted.");
    }

    public async Task<ApiResponse<List<NotificationDto>>> GetAllAsync()
    {
        var notifications = await _repo.GetAllAsync();
        return ApiResponse<List<NotificationDto>>.Ok(notifications.Select(MapToDto).ToList());
    }

    // ════════════════════════════════════════════════
    // PREDEFINED NOTIFICATION HELPERS
    // Called automatically by other services
    // ════════════════════════════════════════════════

    public async Task SendBookingConfirmationAsync(int userId, int bookingId, string spotNumber)
    {
        await _repo.CreateAsync(new Notification
        {
            RecipientId = userId,
            Type = "BOOKING",
            Title = "Booking Confirmed! 🎉",
            Message = $"Your booking for spot {spotNumber} has been confirmed. Booking ID: #{bookingId}",
            Channel = "APP",
            RelatedId = bookingId,
            RelatedType = "BOOKING",
            IsRead = false,
            SentAt = DateTime.UtcNow
        });
        _logger.LogInformation("Booking confirmation sent to user {UserId} for booking {BookingId}", userId, bookingId);
    }

    public async Task SendCheckInAlertAsync(int userId, int bookingId, string spotNumber)
    {
        await _repo.CreateAsync(new Notification
        {
            RecipientId = userId,
            Type = "CHECKIN",
            Title = "Check-In Successful ✅",
            Message = $"You have checked in to spot {spotNumber}. Your parking session has started.",
            Channel = "APP",
            RelatedId = bookingId,
            RelatedType = "BOOKING",
            IsRead = false,
            SentAt = DateTime.UtcNow
        });
        _logger.LogInformation("Check-in alert sent to user {UserId}", userId);
    }

    public async Task SendCheckOutConfirmationAsync(int userId, int bookingId, decimal totalAmount)
    {
        await _repo.CreateAsync(new Notification
        {
            RecipientId = userId,
            Type = "CHECKOUT",
            Title = "Check-Out Complete 🚗",
            Message = $"You have checked out. Total fare: ₹{totalAmount:F2}. Thank you for using ParkEase!",
            Channel = "APP",
            RelatedId = bookingId,
            RelatedType = "BOOKING",
            IsRead = false,
            SentAt = DateTime.UtcNow
        });
        _logger.LogInformation("Check-out confirmation sent to user {UserId}", userId);
    }

    public async Task SendPaymentReceiptAsync(int userId, int paymentId, decimal amount, string mode)
    {
        await _repo.CreateAsync(new Notification
        {
            RecipientId = userId,
            Type = "PAYMENT",
            Title = "Payment Received 💳",
            Message = $"Payment of ₹{amount:F2} received via {mode}. Payment ID: #{paymentId}",
            Channel = "APP",
            RelatedId = paymentId,
            RelatedType = "PAYMENT",
            IsRead = false,
            SentAt = DateTime.UtcNow
        });
        _logger.LogInformation("Payment receipt sent to user {UserId}", userId);
    }

    public async Task SendExpiryReminderAsync(int userId, int bookingId, DateTime endTime)
    {
        await _repo.CreateAsync(new Notification
        {
            RecipientId = userId,
            Type = "EXPIRY",
            Title = "⏰ Parking Expiry Reminder",
            Message = $"Your parking booking #{bookingId} expires at {endTime:HH:mm}. Please check out or extend.",
            Channel = "APP",
            RelatedId = bookingId,
            RelatedType = "BOOKING",
            IsRead = false,
            SentAt = DateTime.UtcNow
        });
        _logger.LogInformation("Expiry reminder sent to user {UserId}", userId);
    }

    // ── Channel Dispatcher ──────────────────────────

    private async Task DispatchToChannelAsync(Notification notification)
    {
        switch (notification.Channel)
        {
            case "EMAIL":
                // Email stub — ready for MailKit integration
                _logger.LogInformation(
                    "EMAIL stub: To user {RecipientId} | Subject: {Title}",
                    notification.RecipientId, notification.Title);
                break;

            case "SMS":
                // SMS stub — ready for Twilio integration
                _logger.LogInformation(
                    "SMS stub: To user {RecipientId} | Message: {Message}",
                    notification.RecipientId, notification.Message);
                break;

            case "APP":
            default:
                // In-app notification — already saved to DB
                _logger.LogInformation(
                    "APP notification saved for user {RecipientId}",
                    notification.RecipientId);
                break;
        }

        await Task.CompletedTask;
    }

    // ── Private Helper ──────────────────────────────
    private static NotificationDto MapToDto(Notification n) => new()
    {
        NotificationId = n.NotificationId,
        RecipientId = n.RecipientId,
        Type = n.Type,
        Title = n.Title,
        Message = n.Message,
        Channel = n.Channel,
        RelatedId = n.RelatedId,
        RelatedType = n.RelatedType,
        IsRead = n.IsRead,
        SentAt = n.SentAt
    };
}
