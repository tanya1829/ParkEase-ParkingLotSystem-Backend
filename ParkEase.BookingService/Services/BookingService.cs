using ParkEase.BookingService.DTOs;
using ParkEase.BookingService.Entities;
using ParkEase.BookingService.Interfaces;

namespace ParkEase.BookingService.Services;

/// <summary>
/// Core orchestration service for the ParkEase platform.
/// Manages complete booking lifecycle and analytics/reporting.
/// Fare formula: (CheckOutTime - CheckInTime in hours) x PricePerHour (min 1 hour)
/// </summary>
public class BookingService : IBookingService
{
    private readonly IBookingRepository _repo;

    public BookingService(IBookingRepository repo)
    {
        _repo = repo;
    }

    // ════════════════════════════════════════════════
    // BOOKING LIFECYCLE
    // ════════════════════════════════════════════════

    public async Task<ApiResponse<BookingDto>> CreateBookingAsync(CreateBookingRequest request)
    {
        // Validate times
        if (request.EndTime <= request.StartTime)
            return ApiResponse<BookingDto>.Fail("EndTime must be after StartTime.");

        if (request.StartTime < DateTime.UtcNow.AddMinutes(-5))
            return ApiResponse<BookingDto>.Fail("StartTime cannot be in the past.");

        if (request.PricePerHour <= 0)
            return ApiResponse<BookingDto>.Fail("PricePerHour must be greater than 0.");

        // Check if spot is already booked
        var existingBooking = await _repo.FindActiveBySpotIdAsync(request.SpotId);
        if (existingBooking != null)
            return ApiResponse<BookingDto>.Fail("Spot is already reserved or occupied.");

        // Calculate estimated fare
        var durationHours = Math.Max(1, (request.EndTime - request.StartTime).TotalHours);
        var estimatedFare = (decimal)durationHours * request.PricePerHour;

        var booking = new Booking
        {
            UserId = request.UserId,
            LotId = request.LotId,
            SpotId = request.SpotId,
            VehiclePlate = request.VehiclePlate.ToUpper(),
            VehicleType = request.VehicleType.ToUpper(),
            BookingType = request.BookingType.ToUpper(),
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Status = "RESERVED",
            PricePerHour = request.PricePerHour,
            TotalAmount = estimatedFare,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repo.CreateAsync(booking);
        return ApiResponse<BookingDto>.Ok(MapToDto(created), "Booking created successfully.");
    }

    public async Task<ApiResponse<BookingDto>> GetBookingByIdAsync(int bookingId)
    {
        var booking = await _repo.FindByBookingIdAsync(bookingId);
        if (booking == null) return ApiResponse<BookingDto>.Fail("Booking not found.");
        return ApiResponse<BookingDto>.Ok(MapToDto(booking));
    }

    public async Task<ApiResponse<List<BookingDto>>> GetBookingsByUserAsync(int userId)
    {
        var bookings = await _repo.FindByUserIdAsync(userId);
        return ApiResponse<List<BookingDto>>.Ok(bookings.Select(MapToDto).ToList());
    }

    public async Task<ApiResponse<List<BookingDto>>> GetBookingsByLotAsync(int lotId)
    {
        var bookings = await _repo.FindByLotIdAsync(lotId);
        return ApiResponse<List<BookingDto>>.Ok(bookings.Select(MapToDto).ToList());
    }

    public async Task<ApiResponse<List<BookingDto>>> GetActiveBookingsAsync(int lotId)
    {
        var bookings = await _repo.FindByLotIdAsync(lotId);
        var active = bookings.Where(b => b.Status == "RESERVED" || b.Status == "ACTIVE").ToList();
        return ApiResponse<List<BookingDto>>.Ok(active.Select(MapToDto).ToList());
    }

    public async Task<ApiResponse<BookingDto>> CancelBookingAsync(int bookingId)
    {
        var booking = await _repo.FindByBookingIdAsync(bookingId);
        if (booking == null) return ApiResponse<BookingDto>.Fail("Booking not found.");

        if (booking.Status == "COMPLETED")
            return ApiResponse<BookingDto>.Fail("Cannot cancel a completed booking.");

        if (booking.Status == "CANCELLED")
            return ApiResponse<BookingDto>.Fail("Booking is already cancelled.");

        booking.Status = "CANCELLED";
        booking.TotalAmount = 0;
        var updated = await _repo.UpdateAsync(booking);
        return ApiResponse<BookingDto>.Ok(MapToDto(updated), "Booking cancelled successfully.");
    }

    public async Task<ApiResponse<BookingDto>> CheckInAsync(int bookingId)
    {
        var booking = await _repo.FindByBookingIdAsync(bookingId);
        if (booking == null) return ApiResponse<BookingDto>.Fail("Booking not found.");

        if (booking.Status != "RESERVED")
            return ApiResponse<BookingDto>.Fail($"Cannot check in. Current status: {booking.Status}");

        // Transition: RESERVED → ACTIVE
        booking.Status = "ACTIVE";
        booking.CheckInTime = DateTime.UtcNow;
        var updated = await _repo.UpdateAsync(booking);

        // Log occupancy for analytics
        await LogOccupancyAsync(booking.LotId);

        return ApiResponse<BookingDto>.Ok(MapToDto(updated), "Check-in successful. Spot is now occupied.");
    }

    public async Task<ApiResponse<BookingDto>> CheckOutAsync(int bookingId)
    {
        var booking = await _repo.FindByBookingIdAsync(bookingId);
        if (booking == null) return ApiResponse<BookingDto>.Fail("Booking not found.");

        if (booking.Status != "ACTIVE")
            return ApiResponse<BookingDto>.Fail($"Cannot check out. Current status: {booking.Status}");

        booking.CheckOutTime = DateTime.UtcNow;
        booking.Status = "COMPLETED";

        // Calculate final fare: (CheckOutTime - CheckInTime) x PricePerHour (min 1 hour)
        var checkIn = booking.CheckInTime ?? booking.StartTime;
        var durationHours = Math.Max(1.0, (booking.CheckOutTime.Value - checkIn).TotalHours);
        booking.TotalAmount = (decimal)durationHours * booking.PricePerHour;

        var updated = await _repo.UpdateAsync(booking);

        // Log occupancy for analytics
        await LogOccupancyAsync(booking.LotId);

        return ApiResponse<BookingDto>.Ok(MapToDto(updated),
            $"Check-out successful. Total fare: ₹{booking.TotalAmount:F2}");
    }

    public async Task<ApiResponse<BookingDto>> ExtendBookingAsync(int bookingId, ExtendBookingRequest request)
    {
        var booking = await _repo.FindByBookingIdAsync(bookingId);
        if (booking == null) return ApiResponse<BookingDto>.Fail("Booking not found.");

        if (booking.Status == "COMPLETED" || booking.Status == "CANCELLED")
            return ApiResponse<BookingDto>.Fail("Cannot extend a completed or cancelled booking.");

        if (request.NewEndTime <= booking.EndTime)
            return ApiResponse<BookingDto>.Fail("New end time must be after current end time.");

        booking.EndTime = request.NewEndTime;

        // Recalculate estimated fare
        var durationHours = Math.Max(1.0, (booking.EndTime - booking.StartTime).TotalHours);
        booking.TotalAmount = (decimal)durationHours * booking.PricePerHour;

        var updated = await _repo.UpdateAsync(booking);
        return ApiResponse<BookingDto>.Ok(MapToDto(updated), "Booking extended successfully.");
    }

    public async Task<ApiResponse<FareDto>> CalculateAmountAsync(int bookingId)
    {
        var booking = await _repo.FindByBookingIdAsync(bookingId);
        if (booking == null) return ApiResponse<FareDto>.Fail("Booking not found.");

        DateTime checkIn = booking.CheckInTime ?? booking.StartTime;
        DateTime checkOut = booking.CheckOutTime ?? DateTime.UtcNow;
        var durationHours = Math.Max(1.0, (checkOut - checkIn).TotalHours);
        var fare = (decimal)durationHours * booking.PricePerHour;

        return ApiResponse<FareDto>.Ok(new FareDto
        {
            BookingId = bookingId,
            PricePerHour = booking.PricePerHour,
            DurationHours = Math.Round(durationHours, 2),
            TotalFare = Math.Round(fare, 2),
            Note = durationHours < 1 ? "Minimum 1 hour charge applied." : string.Empty
        });
    }

    public async Task<ApiResponse<List<BookingDto>>> GetBookingHistoryAsync(int userId)
    {
        var bookings = await _repo.FindByUserIdAsync(userId);
        var completed = bookings
            .Where(b => b.Status == "COMPLETED" || b.Status == "CANCELLED")
            .ToList();
        return ApiResponse<List<BookingDto>>.Ok(completed.Select(MapToDto).ToList());
    }

    // ════════════════════════════════════════════════
    // ANALYTICS
    // ════════════════════════════════════════════════

    public async Task<ApiResponse<OccupancyDto>> GetOccupancyRateAsync(int lotId, int totalSpots)
    {
        var activeCount = await _repo.CountByLotIdAndStatusAsync(lotId, "ACTIVE");
        var reservedCount = await _repo.CountByLotIdAndStatusAsync(lotId, "RESERVED");
        var occupied = activeCount + reservedCount;
        var rate = totalSpots > 0 ? Math.Round((double)occupied / totalSpots * 100, 2) : 0;

        return ApiResponse<OccupancyDto>.Ok(new OccupancyDto
        {
            LotId = lotId,
            OccupiedSpots = occupied,
            TotalSpots = totalSpots,
            OccupancyRate = rate,
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task<ApiResponse<RevenueDto>> GetRevenueAsync(int lotId, DateTime from, DateTime to)
    {
        var bookings = await _repo.FindByLotIdAndDateRangeAsync(lotId, from, to);

        var dailyBreakdown = bookings
            .GroupBy(b => b.CheckOutTime?.Date ?? b.CreatedAt.Date)
            .Select(g => new DailyRevenueDto
            {
                Date = g.Key,
                Revenue = g.Sum(b => b.TotalAmount),
                Bookings = g.Count()
            })
            .OrderBy(d => d.Date)
            .ToList();

        return ApiResponse<RevenueDto>.Ok(new RevenueDto
        {
            LotId = lotId,
            TotalRevenue = bookings.Sum(b => b.TotalAmount),
            TotalBookings = bookings.Count,
            FromDate = from,
            ToDate = to,
            DailyBreakdown = dailyBreakdown
        });
    }

    public async Task<ApiResponse<PeakHoursDto>> GetPeakHoursAsync(int lotId)
    {
        var bookings = await _repo.FindCompletedByLotIdAsync(lotId);

        var hourlyBreakdown = bookings
            .Where(b => b.CheckInTime.HasValue)
            .GroupBy(b => b.CheckInTime!.Value.Hour)
            .Select(g => new HourlyBookingDto
            {
                Hour = g.Key,
                BookingCount = g.Count()
            })
            .OrderBy(h => h.Hour)
            .ToList();

        var peakHour = hourlyBreakdown.OrderByDescending(h => h.BookingCount)
            .FirstOrDefault()?.Hour ?? 0;

        return ApiResponse<PeakHoursDto>.Ok(new PeakHoursDto
        {
            LotId = lotId,
            HourlyBreakdown = hourlyBreakdown,
            PeakHour = peakHour
        });
    }

    public async Task<ApiResponse<PlatformSummaryDto>> GetPlatformSummaryAsync()
    {
        var allBookings = await _repo.FindByStatusAsync("COMPLETED");
        var activeBookings = await _repo.FindByStatusAsync("ACTIVE");
        var reservedBookings = await _repo.FindByStatusAsync("RESERVED");
        var cancelledBookings = await _repo.FindByStatusAsync("CANCELLED");

        var completedWithDuration = allBookings
            .Where(b => b.CheckInTime.HasValue && b.CheckOutTime.HasValue)
            .ToList();

        var avgDuration = completedWithDuration.Any()
            ? completedWithDuration.Average(b =>
                (b.CheckOutTime!.Value - b.CheckInTime!.Value).TotalHours)
            : 0;

        return ApiResponse<PlatformSummaryDto>.Ok(new PlatformSummaryDto
        {
            TotalBookings = allBookings.Count + activeBookings.Count +
                            reservedBookings.Count + cancelledBookings.Count,
            ActiveBookings = activeBookings.Count + reservedBookings.Count,
            CompletedBookings = allBookings.Count,
            CancelledBookings = cancelledBookings.Count,
            TotalRevenue = allBookings.Sum(b => b.TotalAmount),
            AverageParkingDurationHours = Math.Round(avgDuration, 2)
        });
    }

    // ── Private Helpers ──────────────────────────────

    private async Task LogOccupancyAsync(int lotId)
    {
        var active = await _repo.CountByLotIdAndStatusAsync(lotId, "ACTIVE");
        var reserved = await _repo.CountByLotIdAndStatusAsync(lotId, "RESERVED");
        var occupied = active + reserved;

        await _repo.CreateOccupancyLogAsync(new OccupancyLog
        {
            LotId = lotId,
            OccupiedSpots = occupied,
            TotalSpots = 0, // updated by lot service
            OccupancyRate = 0,
            Timestamp = DateTime.UtcNow
        });
    }

    private static BookingDto MapToDto(Booking b) => new()
    {
        BookingId = b.BookingId,
        UserId = b.UserId,
        LotId = b.LotId,
        SpotId = b.SpotId,
        VehiclePlate = b.VehiclePlate,
        VehicleType = b.VehicleType,
        BookingType = b.BookingType,
        StartTime = b.StartTime,
        EndTime = b.EndTime,
        CheckInTime = b.CheckInTime,
        CheckOutTime = b.CheckOutTime,
        Status = b.Status,
        TotalAmount = b.TotalAmount,
        PricePerHour = b.PricePerHour,
        CreatedAt = b.CreatedAt
    };
}
