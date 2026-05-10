using ParkEase.BookingService.DTOs;

namespace ParkEase.BookingService.Interfaces;

/// <summary>Business logic contract for booking lifecycle and analytics</summary>
public interface IBookingService
{
    // ── Booking Lifecycle ──
    Task<ApiResponse<BookingDto>> CreateBookingAsync(CreateBookingRequest request);
    Task<ApiResponse<BookingDto>> GetBookingByIdAsync(int bookingId);
    Task<ApiResponse<List<BookingDto>>> GetBookingsByUserAsync(int userId);
    Task<ApiResponse<List<BookingDto>>> GetBookingsByLotAsync(int lotId);
    Task<ApiResponse<List<BookingDto>>> GetActiveBookingsAsync(int lotId);
    Task<ApiResponse<BookingDto>> CancelBookingAsync(int bookingId);
    Task<ApiResponse<BookingDto>> CheckInAsync(int bookingId);
    Task<ApiResponse<BookingDto>> CheckOutAsync(int bookingId);
    Task<ApiResponse<BookingDto>> ExtendBookingAsync(int bookingId, ExtendBookingRequest request);
    Task<ApiResponse<FareDto>> CalculateAmountAsync(int bookingId);
    Task<ApiResponse<List<BookingDto>>> GetBookingHistoryAsync(int userId);

    // ── Analytics ──
    Task<ApiResponse<OccupancyDto>> GetOccupancyRateAsync(int lotId, int totalSpots);
    Task<ApiResponse<RevenueDto>> GetRevenueAsync(int lotId, DateTime from, DateTime to);
    Task<ApiResponse<PeakHoursDto>> GetPeakHoursAsync(int lotId);
    Task<ApiResponse<PlatformSummaryDto>> GetPlatformSummaryAsync();
}
