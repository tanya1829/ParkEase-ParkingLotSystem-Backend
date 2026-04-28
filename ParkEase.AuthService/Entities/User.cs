namespace ParkEase.AuthService.Entities;

public class User
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Role { get; set; } = "DRIVER"; // DRIVER | MANAGER | ADMIN
    public string? VehiclePlate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? ProfilePicUrl { get; set; }
    public string? OAuthProvider { get; set; }   // google | github | null
    public string? OAuthProviderId { get; set; }
}
