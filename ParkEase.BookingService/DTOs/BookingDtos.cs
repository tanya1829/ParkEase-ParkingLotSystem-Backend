namespace ParkEase.BookingService.DTOs;

// ---------- Request DTOs ----------

/// <summary>Request to create a new booking</summary>
public class CreateBookingRequest
{
    public int UserId { get; set; }
    public int LotId { get; set; }
    public int SpotId { get; set; }
    public string VehiclePlate { get; set; } = string.Empty;
    public string VehicleType { get; set; } = "4W";
    public string BookingType { get; set; } = "PRE";   // PRE | WALK_IN
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public decimal PricePerHour { get; set; }
}

/// <summary>Request to extend booking duration</summary>
public class ExtendBookingRequest
{
    public DateTime NewEndTime { get; set; }
}

// ---------- Response DTOs ----------

/// <summary>Booking details returned in API responses</summary>
public class BookingDto
{
    public int BookingId { get; set; }
    public int UserId { get; set; }
    public int LotId { get; set; }
    public int SpotId { get; set; }
    public string VehiclePlate { get; set; } = string.Empty;
    public string VehicleType { get; set; } = string.Empty;
    public string BookingType { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal PricePerHour { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Fare calculation result</summary>
public class FareDto
{
    public int BookingId { get; set; }
    public decimal PricePerHour { get; set; }
    public double DurationHours { get; set; }
    public decimal TotalFare { get; set; }
    public string Note { get; set; } = string.Empty;
}

// ---------- Analytics DTOs ----------

/// <summary>Real-time occupancy data for a lot</summary>
public class OccupancyDto
{
    public int LotId { get; set; }
    public int OccupiedSpots { get; set; }
    public int TotalSpots { get; set; }
    public double OccupancyRate { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>Revenue summary for a lot over a date range</summary>
public class RevenueDto
{
    public int LotId { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalBookings { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<DailyRevenueDto> DailyBreakdown { get; set; } = new();
}

/// <summary>Revenue for a single day</summary>
public class DailyRevenueDto
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public int Bookings { get; set; }
}

/// <summary>Peak hours analysis for a lot</summary>
public class PeakHoursDto
{
    public int LotId { get; set; }
    public List<HourlyBookingDto> HourlyBreakdown { get; set; } = new();
    public int PeakHour { get; set; }
}

/// <summary>Bookings per hour</summary>
public class HourlyBookingDto
{
    public int Hour { get; set; }       // 0-23
    public int BookingCount { get; set; }
}

/// <summary>Platform-wide summary for admin dashboard</summary>
public class PlatformSummaryDto
{
    public int TotalBookings { get; set; }
    public int ActiveBookings { get; set; }
    public int CompletedBookings { get; set; }
    public int CancelledBookings { get; set; }
    public decimal TotalRevenue { get; set; }
    public double AverageParkingDurationHours { get; set; }
}

/// <summary>Generic API response wrapper</summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Success") =>
        new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message) =>
        new() { Success = false, Message = message };
}
