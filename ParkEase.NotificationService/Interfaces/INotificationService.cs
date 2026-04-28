using ParkEase.NotificationService.DTOs;

namespace ParkEase.NotificationService.Interfaces;

/// <summary>Business logic contract for notification operations</summary>
public interface INotificationService
{
    Task<ApiResponse<NotificationDto>> SendAsync(SendNotificationRequest request);
    Task<ApiResponse<List<NotificationDto>>> SendBulkAsync(SendBulkNotificationRequest request);
    Task<ApiResponse<List<NotificationDto>>> GetByRecipientAsync(int recipientId);
    Task<ApiResponse<NotificationDto>> MarkAsReadAsync(int notificationId);
    Task<ApiResponse<string>> MarkAllReadAsync(int recipientId);
    Task<ApiResponse<int>> GetUnreadCountAsync(int recipientId);
    Task<ApiResponse<string>> DeleteNotificationAsync(int notificationId);
    Task<ApiResponse<List<NotificationDto>>> GetAllAsync();

    // ── Predefined notification helpers ──
    Task SendBookingConfirmationAsync(int userId, int bookingId, string spotNumber);
    Task SendCheckInAlertAsync(int userId, int bookingId, string spotNumber);
    Task SendCheckOutConfirmationAsync(int userId, int bookingId, decimal totalAmount);
    Task SendPaymentReceiptAsync(int userId, int paymentId, decimal amount, string mode);
    Task SendExpiryReminderAsync(int userId, int bookingId, DateTime endTime);
}
