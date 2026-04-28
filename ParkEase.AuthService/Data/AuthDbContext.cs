using Microsoft.EntityFrameworkCore;
using ParkEase.AuthService.Entities;

namespace ParkEase.AuthService.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.UserId);
            entity.Property(u => u.UserId).UseIdentityAlwaysColumn();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
            entity.Property(u => u.FullName).IsRequired().HasMaxLength(150);
            entity.Property(u => u.PasswordHash).HasMaxLength(500);
            entity.Property(u => u.Phone).HasMaxLength(20);
            entity.Property(u => u.Role).HasMaxLength(20).HasDefaultValue("DRIVER");
            entity.Property(u => u.VehiclePlate).HasMaxLength(20);
            entity.Property(u => u.ProfilePicUrl).HasMaxLength(500);
            entity.Property(u => u.OAuthProvider).HasMaxLength(50);
            entity.Property(u => u.OAuthProviderId).HasMaxLength(200);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id).UseIdentityAlwaysColumn();
            entity.HasIndex(r => r.Token).IsUnique();
            entity.HasOne<User>()
                  .WithMany()
                  .HasForeignKey(r => r.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
