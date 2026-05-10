using Microsoft.EntityFrameworkCore;
using ParkEase.BookingService.Data;
using ParkEase.BookingService.Entities;
using ParkEase.BookingService.Interfaces;

namespace ParkEase.BookingService.Repositories;

/// <summary>EF Core implementation of booking data access</summary>
public class BookingRepository : IBookingRepository
{
    private readonly BookingDbContext _context;

    public BookingRepository(BookingDbContext context)
    {
        _context = context;
    }

    public async Task<List<Booking>> FindByUserIdAsync(int userId) =>
        await _context.Bookings
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

    public async Task<List<Booking>> FindByLotIdAsync(int lotId) =>
        await _context.Bookings
            .Where(b => b.LotId == lotId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

    public async Task<List<Booking>> FindBySpotIdAsync(int spotId) =>
        await _context.Bookings
            .Where(b => b.SpotId == spotId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

    public async Task<List<Booking>> FindByStatusAsync(string status) =>
        await _context.Bookings
            .Where(b => b.Status == status)
            .ToListAsync();

    public async Task<Booking?> FindByBookingIdAsync(int bookingId) =>
        await _context.Bookings.FindAsync(bookingId);

    public async Task<Booking?> FindActiveBySpotIdAsync(int spotId) =>
        await _context.Bookings
            .FirstOrDefaultAsync(b => b.SpotId == spotId &&
                (b.Status == "RESERVED" || b.Status == "ACTIVE"));

    public async Task<List<Booking>> FindByVehiclePlateAsync(string plate) =>
        await _context.Bookings
            .Where(b => b.VehiclePlate == plate)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

    public async Task<int> CountByLotIdAndStatusAsync(int lotId, string status) =>
        await _context.Bookings
            .CountAsync(b => b.LotId == lotId && b.Status == status);

    public async Task<List<Booking>> FindCompletedByLotIdAsync(int lotId) =>
        await _context.Bookings
            .Where(b => b.LotId == lotId && b.Status == "COMPLETED")
            .ToListAsync();

    public async Task<List<Booking>> FindByLotIdAndDateRangeAsync(int lotId, DateTime from, DateTime to) =>
        await _context.Bookings
            .Where(b => b.LotId == lotId &&
                        b.CreatedAt >= from &&
                        b.CreatedAt <= to &&
                        b.Status == "COMPLETED")
            .ToListAsync();

    public async Task<Booking> CreateAsync(Booking booking)
    {
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();
        return booking;
    }

    public async Task<Booking> UpdateAsync(Booking booking)
    {
        booking.UpdatedAt = DateTime.UtcNow;
        _context.Bookings.Update(booking);
        await _context.SaveChangesAsync();
        return booking;
    }

    public async Task<List<OccupancyLog>> GetOccupancyLogsAsync(int lotId) =>
        await _context.OccupancyLogs
            .Where(o => o.LotId == lotId)
            .OrderByDescending(o => o.Timestamp)
            .Take(100)
            .ToListAsync();

    public async Task CreateOccupancyLogAsync(OccupancyLog log)
    {
        _context.OccupancyLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}
