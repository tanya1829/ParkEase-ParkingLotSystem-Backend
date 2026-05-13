using Microsoft.EntityFrameworkCore;
using ParkEase.ParkingLotService.Entities;

namespace ParkEase.ParkingLotService.Data;

public class ParkingLotDbContext : DbContext
{
    public ParkingLotDbContext(DbContextOptions<ParkingLotDbContext> options) : base(options) { }

    public DbSet<ParkingLot> ParkingLots => Set<ParkingLot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("lots"); // ← ADDED

        modelBuilder.Entity<ParkingLot>(entity =>
        {
            entity.HasKey(p => p.LotId);
            entity.Property(p => p.LotId).UseIdentityAlwaysColumn();
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Address).IsRequired().HasMaxLength(500);
            entity.Property(p => p.City).IsRequired().HasMaxLength(100);
            entity.Property(p => p.ImageUrl).HasMaxLength(500);
            entity.Property(p => p.Description).HasMaxLength(1000);
            entity.Property(p => p.IsOpen).HasDefaultValue(false);
            entity.Property(p => p.IsApproved).HasDefaultValue(false);
            entity.Property(p => p.RowVersion).IsRowVersion();

            entity.HasIndex(p => p.City);
            entity.HasIndex(p => p.ManagerId);
            entity.HasIndex(p => p.IsApproved);
        });
    }
}