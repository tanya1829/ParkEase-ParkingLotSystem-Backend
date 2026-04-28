using Microsoft.EntityFrameworkCore;
using ParkEase.SpotService.Entities;

namespace ParkEase.SpotService.Data;

/// <summary>
/// EF Core DbContext for Spot Service.
/// Uses its own database: parkease_spots
/// </summary>
public class SpotDbContext : DbContext
{
    public SpotDbContext(DbContextOptions<SpotDbContext> options) : base(options) { }

    public DbSet<ParkingSpot> ParkingSpots => Set<ParkingSpot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ParkingSpot>(entity =>
        {
            entity.HasKey(s => s.SpotId);
            entity.Property(s => s.SpotId).UseIdentityAlwaysColumn();
            entity.Property(s => s.SpotNumber).IsRequired().HasMaxLength(20);
            entity.Property(s => s.SpotType).IsRequired().HasMaxLength(20).HasDefaultValue("STANDARD");
            entity.Property(s => s.VehicleType).IsRequired().HasMaxLength(10).HasDefaultValue("4W");
            entity.Property(s => s.Status).IsRequired().HasMaxLength(20).HasDefaultValue("AVAILABLE");
            entity.Property(s => s.PricePerHour).HasColumnType("decimal(10,2)");
            entity.Property(s => s.IsHandicapped).HasDefaultValue(false);
            entity.Property(s => s.IsEVCharging).HasDefaultValue(false);

            // Unique constraint: same lot cannot have duplicate spot numbers
            entity.HasIndex(s => new { s.LotId, s.SpotNumber }).IsUnique();

            // Indexes for fast filtering
            entity.HasIndex(s => s.LotId);
            entity.HasIndex(s => s.Status);
            entity.HasIndex(s => s.SpotType);
            entity.HasIndex(s => s.VehicleType);
        });
    }
}
