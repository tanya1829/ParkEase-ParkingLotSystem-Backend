namespace ParkEase.BookingService.Entities;

/// <summary>
/// Periodic snapshot of lot occupancy for analytics and reporting.
/// Written on every check-in and check-out event.
/// </summary>
public class OccupancyLog
{
    public int LogId { get; set; }
    public int LotId { get; set; }
    public int OccupiedSpots { get; set; }
    public int TotalSpots { get; set; }
    public double OccupancyRate { get; set; }  // (occupied/total) * 100
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
