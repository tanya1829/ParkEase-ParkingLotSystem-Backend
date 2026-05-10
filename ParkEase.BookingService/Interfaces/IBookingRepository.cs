using ParkEase.BookingService.Entities;

namespace ParkEase.BookingService.Interfaces;

/// <summary>Data access contract for booking operations</summary>
public interface IBookingRepository
{
    Task<List<Booking>> FindByUserIdAsync(int userId);
    Task<List<Booking>> FindByLotIdAsync(int lotId);
    Task<List<Booking>> FindBySpotIdAsync(int spotId);
    Task<List<Booking>> FindByStatusAsync(string status);
    Task<Booking?> FindByBookingIdAsync(int bookingId);
    Task<Booking?> FindActiveBySpotIdAsync(int spotId);
    Task<List<Booking>> FindByVehiclePlateAsync(string plate);
    Task<int> CountByLotIdAndStatusAsync(int lotId, string status);
    Task<List<Booking>> FindCompletedByLotIdAsync(int lotId);
    Task<List<Booking>> FindByLotIdAndDateRangeAsync(int lotId, DateTime from, DateTime to);
    Task<Booking> CreateAsync(Booking booking);
    Task<Booking> UpdateAsync(Booking booking);
    Task<List<OccupancyLog>> GetOccupancyLogsAsync(int lotId);
    Task CreateOccupancyLogAsync(OccupancyLog log);
}
