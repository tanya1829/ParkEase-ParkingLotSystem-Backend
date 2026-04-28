using Microsoft.EntityFrameworkCore;
using ParkEase.PaymentService.Entities;

namespace ParkEase.PaymentService.Data;

/// <summary>
/// EF Core DbContext for Payment Service.
/// Uses its own database: parkease_payments
/// </summary>
public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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

            // Indexes
            entity.HasIndex(p => p.BookingId).IsUnique(); // one payment per booking
            entity.HasIndex(p => p.UserId);
            entity.HasIndex(p => p.Status);
            entity.HasIndex(p => p.TransactionId);
        });
    }
}
