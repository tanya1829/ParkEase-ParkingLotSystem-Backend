namespace ParkEase.BookingService.Entities;

/// <summary>
/// Represents a parking booking linking a driver, spot, and time window.
/// Status flow: RESERVED → ACTIVE (check-in) → COMPLETED (checkout) | CANCELLED
/// </summary>
public class Booking
{
    public int BookingId { get; set; }
    public int UserId { get; set; }           // Driver's UserId from Auth Service
    public int LotId { get; set; }            // From ParkingLot Service
    public int SpotId { get; set; }           // From Spot Service
    public string VehiclePlate { get; set; } = string.Empty;
    public string VehicleType { get; set; } = string.Empty;  // 2W | 4W | HEAVY
    public string BookingType { get; set; } = "PRE";          // PRE | WALK_IN
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string Status { get; set; } = "RESERVED";          // RESERVED | ACTIVE | COMPLETED | CANCELLED
    public decimal TotalAmount { get; set; } = 0;
    public decimal PricePerHour { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
