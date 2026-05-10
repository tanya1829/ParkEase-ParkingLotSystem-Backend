using Microsoft.EntityFrameworkCore;
using ParkEase.BookingService.Entities;

namespace ParkEase.BookingService.Data;

/// <summary>
/// EF Core DbContext for Booking Service.
/// Uses its own database: parkease_bookings
/// Also stores OccupancyLogs for analytics.
/// </summary>
public class BookingDbContext : DbContext
{
    public BookingDbContext(DbContextOptions<BookingDbContext> options) : base(options) { }

    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<OccupancyLog> OccupancyLogs => Set<OccupancyLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(b => b.BookingId);
            entity.Property(b => b.BookingId).UseIdentityAlwaysColumn();
            entity.Property(b => b.VehiclePlate).IsRequired().HasMaxLength(20);
            entity.Property(b => b.VehicleType).IsRequired().HasMaxLength(10);
            entity.Property(b => b.BookingType).IsRequired().HasMaxLength(10).HasDefaultValue("PRE");
            entity.Property(b => b.Status).IsRequired().HasMaxLength(20).HasDefaultValue("RESERVED");
            entity.Property(b => b.TotalAmount).HasColumnType("decimal(10,2)");
            entity.Property(b => b.PricePerHour).HasColumnType("decimal(10,2)");

            // Indexes for fast querying
            entity.HasIndex(b => b.UserId);
            entity.HasIndex(b => b.LotId);
            entity.HasIndex(b => b.SpotId);
            entity.HasIndex(b => b.Status);
            entity.HasIndex(b => b.VehiclePlate);
        });

        modelBuilder.Entity<OccupancyLog>(entity =>
        {
            entity.HasKey(o => o.LogId);
            entity.Property(o => o.LogId).UseIdentityAlwaysColumn();
            entity.Property(o => o.OccupancyRate).HasColumnType("double precision");
            entity.HasIndex(o => o.LotId);
            entity.HasIndex(o => o.Timestamp);
        });
    }
}
