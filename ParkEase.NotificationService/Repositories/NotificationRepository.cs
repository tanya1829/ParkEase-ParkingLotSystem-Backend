using Microsoft.EntityFrameworkCore;
using ParkEase.NotificationService.Data;
using ParkEase.NotificationService.Entities;
using ParkEase.NotificationService.Interfaces;

namespace ParkEase.NotificationService.Repositories;

/// <summary>EF Core implementation of notification data access</summary>
public class NotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _context;

    public NotificationRepository(NotificationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Notification>> FindByRecipientIdAsync(int recipientId) =>
        await _context.Notifications
            .Where(n => n.RecipientId == recipientId)
            .OrderByDescending(n => n.SentAt)
            .ToListAsync();

    public async Task<List<Notification>> FindByRecipientIdAndIsReadAsync(int recipientId, bool isRead) =>
        await _context.Notifications
            .Where(n => n.RecipientId == recipientId && n.IsRead == isRead)
            .OrderByDescending(n => n.SentAt)
            .ToListAsync();

    public async Task<int> CountByRecipientIdAndIsReadAsync(int recipientId, bool isRead) =>
        await _context.Notifications
            .CountAsync(n => n.RecipientId == recipientId && n.IsRead == isRead);

    public async Task<List<Notification>> FindByTypeAsync(string type) =>
        await _context.Notifications
            .Where(n => n.Type == type)
            .OrderByDescending(n => n.SentAt)
            .ToListAsync();

    public async Task<Notification?> FindByNotificationIdAsync(int notificationId) =>
        await _context.Notifications.FindAsync(notificationId);

    public async Task<List<Notification>> FindByRelatedIdAsync(int relatedId) =>
        await _context.Notifications
            .Where(n => n.RelatedId == relatedId)
            .ToListAsync();

    public async Task<Notification> CreateAsync(Notification notification)
    {
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
        return notification;
    }

    public async Task<List<Notification>> CreateBulkAsync(List<Notification> notifications)
    {
        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync();
        return notifications;
    }

    public async Task<Notification> UpdateAsync(Notification notification)
    {
        _context.Notifications.Update(notification);
        await _context.SaveChangesAsync();
        return notification;
    }

    public async Task DeleteByNotificationIdAsync(int notificationId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification != null)
        {
            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
        }
    }

    public async Task MarkAllReadByRecipientIdAsync(int recipientId)
    {
        var unread = await _context.Notifications
            .Where(n => n.RecipientId == recipientId && !n.IsRead)
            .ToListAsync();

        unread.ForEach(n => n.IsRead = true);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Notification>> GetAllAsync() =>
        await _context.Notifications
            .OrderByDescending(n => n.SentAt)
            .ToListAsync();
}
