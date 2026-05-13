using Microsoft.EntityFrameworkCore;
using ParkEase.PaymentService.Entities;

namespace ParkEase.PaymentService.Data;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("payments"); // ← ADDED

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(p => p.PaymentId);
            entity.Property(p => p.PaymentId).UseIdentityAlwaysColumn();
            entity.Property(p => p.Amount).HasColumnType("decimal(10,2)");
            entity.Property(p => p.Status).IsRequired().HasMaxLength(20).HasDefaultValue("PENDING");
            entity.Property(p => p.Mode).IsRequired().HasMaxLength(20).HasDefaultValue("CASH");
            entity.Property(p => p.Currency).HasMaxLength(10).HasDefaultValue("INR");
            entity.Property(p => p.TransactionId).HasMaxLength(200);
            entity.Property(p => p.Description).HasMaxLength(500);

            entity.HasIndex(p => p.BookingId).IsUnique();
            entity.HasIndex(p => p.UserId);
            entity.HasIndex(p => p.Status);
            entity.HasIndex(p => p.TransactionId);
        });
    }
}