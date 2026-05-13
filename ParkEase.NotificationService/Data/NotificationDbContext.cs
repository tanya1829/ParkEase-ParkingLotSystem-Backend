using Microsoft.EntityFrameworkCore;
using ParkEase.NotificationService.Entities;

namespace ParkEase.NotificationService.Data;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("notifications"); // ← ADDED

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(n => n.NotificationId);
            entity.Property(n => n.NotificationId).UseIdentityAlwaysColumn();
            entity.Property(n => n.Type).IsRequired().HasMaxLength(30);
            entity.Property(n => n.Title).IsRequired().HasMaxLength(200);
            entity.Property(n => n.Message).IsRequired().HasMaxLength(1000);
            entity.Property(n => n.Channel).IsRequired().HasMaxLength(10).HasDefaultValue("APP");
            entity.Property(n => n.RelatedType).HasMaxLength(20);
            entity.Property(n => n.IsRead).HasDefaultValue(false);

            entity.HasIndex(n => n.RecipientId);
            entity.HasIndex(n => n.IsRead);
            entity.HasIndex(n => n.Type);
            entity.HasIndex(n => n.SentAt);
        });
    }
}