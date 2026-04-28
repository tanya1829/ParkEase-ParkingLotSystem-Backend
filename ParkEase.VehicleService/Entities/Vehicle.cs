namespace ParkEase.VehicleService.Entities;

/// <summary>
/// Represents a vehicle registered by a driver on ParkEase.
/// A driver can register multiple vehicles.
/// </summary>
public class Vehicle
{
    public int VehicleId { get; set; }
    public int OwnerId { get; set; }                        // UserId from Auth Service
    public string LicensePlate { get; set; } = string.Empty; // unique per owner
    public string Make { get; set; } = string.Empty;        // e.g. Honda, Toyota
    public string Model { get; set; } = string.Empty;       // e.g. City, Innova
    public string Color { get; set; } = string.Empty;
    public string VehicleType { get; set; } = "4W";         // 2W | 4W | HEAVY
    public bool IsEV { get; set; } = false;                 // electric vehicle flag
    public bool IsActive { get; set; } = true;
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
