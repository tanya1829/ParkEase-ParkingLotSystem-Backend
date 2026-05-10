using Microsoft.EntityFrameworkCore;
using ParkEase.VehicleService.Entities;

namespace ParkEase.VehicleService.Data;

/// <summary>
/// EF Core DbContext for Vehicle Service.
/// Uses its own database: parkease_vehicles
/// </summary>
public class VehicleDbContext : DbContext
{
    public VehicleDbContext(DbContextOptions<VehicleDbContext> options) : base(options) { }

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasKey(v => v.VehicleId);
            entity.Property(v => v.VehicleId).UseIdentityAlwaysColumn();
            entity.Property(v => v.LicensePlate).IsRequired().HasMaxLength(20);
            entity.Property(v => v.Make).IsRequired().HasMaxLength(100);
            entity.Property(v => v.Model).IsRequired().HasMaxLength(100);
            entity.Property(v => v.Color).HasMaxLength(50);
            entity.Property(v => v.VehicleType).IsRequired().HasMaxLength(10).HasDefaultValue("4W");
            entity.Property(v => v.IsEV).HasDefaultValue(false);
            entity.Property(v => v.IsActive).HasDefaultValue(true);

            // Unique license plate per owner
            entity.HasIndex(v => new { v.OwnerId, v.LicensePlate }).IsUnique();

            // Indexes for fast lookup
            entity.HasIndex(v => v.OwnerId);
            entity.HasIndex(v => v.LicensePlate);
            entity.HasIndex(v => v.VehicleType);
        });
    }
}
