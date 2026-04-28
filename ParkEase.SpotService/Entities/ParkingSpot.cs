namespace ParkEase.SpotService.Entities;

/// <summary>
/// Represents an individual parking space within a lot.
/// Status transitions: AVAILABLE → RESERVED → OCCUPIED → AVAILABLE
/// </summary>
public class ParkingSpot
{
    public int SpotId { get; set; }
    public int LotId { get; set; }
    public string SpotNumber { get; set; } = string.Empty;  // e.g. "A1", "B12"
    public int Floor { get; set; } = 1;
    public string SpotType { get; set; } = "STANDARD";      // COMPACT | STANDARD | LARGE | MOTORBIKE | EV
    public string VehicleType { get; set; } = "4W";         // 2W | 4W | HEAVY
    public string Status { get; set; } = "AVAILABLE";       // AVAILABLE | RESERVED | OCCUPIED
    public bool IsHandicapped { get; set; } = false;
    public bool IsEVCharging { get; set; } = false;
    public decimal PricePerHour { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
