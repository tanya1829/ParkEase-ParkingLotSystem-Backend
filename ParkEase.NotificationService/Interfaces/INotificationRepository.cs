using ParkEase.NotificationService.Entities;

namespace ParkEase.NotificationService.Interfaces;

/// <summary>Data access contract for notification operations</summary>
public interface INotificationRepository
{
    Task<List<Notification>> FindByRecipientIdAsync(int recipientId);
    Task<List<Notification>> FindByRecipientIdAndIsReadAsync(int recipientId, bool isRead);
    Task<int> CountByRecipientIdAndIsReadAsync(int recipientId, bool isRead);
    Task<List<Notification>> FindByTypeAsync(string type);
    Task<Notification?> FindByNotificationIdAsync(int notificationId);
    Task<List<Notification>> FindByRelatedIdAsync(int relatedId);
    Task<Notification> CreateAsync(Notification notification);
    Task<List<Notification>> CreateBulkAsync(List<Notification> notifications);
    Task<Notification> UpdateAsync(Notification notification);
    Task DeleteByNotificationIdAsync(int notificationId);
    Task MarkAllReadByRecipientIdAsync(int recipientId);
    Task<List<Notification>> GetAllAsync();
}
