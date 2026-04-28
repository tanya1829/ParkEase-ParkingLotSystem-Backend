using Microsoft.EntityFrameworkCore;
using ParkEase.ParkingLotService.Data;
using ParkEase.ParkingLotService.Entities;
using ParkEase.ParkingLotService.Interfaces;

namespace ParkEase.ParkingLotService.Repositories;

public class ParkingLotRepository : IParkingLotRepository
{
    private readonly ParkingLotDbContext _context;

    public ParkingLotRepository(ParkingLotDbContext context)
    {
        _context = context;
    }

    public async Task<ParkingLot?> FindByLotIdAsync(int lotId) =>
        await _context.ParkingLots.FindAsync(lotId);

    public async Task<List<ParkingLot>> FindByCityAsync(string city) =>
        await _context.ParkingLots
            .Where(p => p.City.ToLower() == city.ToLower() && p.IsApproved)
            .ToListAsync();

    public async Task<List<ParkingLot>> FindByManagerIdAsync(int managerId) =>
        await _context.ParkingLots
            .Where(p => p.ManagerId == managerId)
            .ToListAsync();

    public async Task<List<ParkingLot>> FindByIsOpenAsync(bool isOpen) =>
        await _context.ParkingLots
            .Where(p => p.IsOpen == isOpen && p.IsApproved)
            .ToListAsync();

    // Haversine formula — finds lots within radiusKm of given coordinates
    public async Task<List<ParkingLot>> FindNearbyAsync(double lat, double lng, double radiusKm)
    {
        // Load approved lots and filter in memory using Haversine
        var allLots = await _context.ParkingLots
            .Where(p => p.IsApproved && p.IsOpen)
            .ToListAsync();

        return allLots
            .Where(lot => CalculateDistanceKm(lat, lng, lot.Latitude, lot.Longitude) <= radiusKm)
            .OrderBy(lot => CalculateDistanceKm(lat, lng, lot.Latitude, lot.Longitude))
            .ToList();
    }

    public async Task<List<ParkingLot>> FindByAvailableSpotsGreaterThanAsync(int minSpots) =>
        await _context.ParkingLots
            .Where(p => p.AvailableSpots > minSpots && p.IsApproved && p.IsOpen)
            .ToListAsync();

    public async Task<int> CountByCityAsync(string city) =>
        await _context.ParkingLots
            .CountAsync(p => p.City.ToLower() == city.ToLower() && p.IsApproved);

    public async Task<List<ParkingLot>> FindPendingApprovalAsync() =>
        await _context.ParkingLots
            .Where(p => !p.IsApproved)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();

    public async Task<List<ParkingLot>> GetAllAsync() =>
        await _context.ParkingLots.ToListAsync();

    public async Task<ParkingLot> CreateAsync(ParkingLot lot)
    {
        _context.ParkingLots.Add(lot);
        await _context.SaveChangesAsync();
        return lot;
    }

    public async Task<ParkingLot> UpdateAsync(ParkingLot lot)
    {
        lot.UpdatedAt = DateTime.UtcNow;
        _context.ParkingLots.Update(lot);
        await _context.SaveChangesAsync();
        return lot;
    }

    public async Task DeleteByLotIdAsync(int lotId)
    {
        var lot = await _context.ParkingLots.FindAsync(lotId);
        if (lot != null)
        {
            _context.ParkingLots.Remove(lot);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<ParkingLot>> SearchAsync(string keyword) =>
        await _context.ParkingLots
            .Where(p => p.IsApproved &&
                (p.Name.ToLower().Contains(keyword.ToLower()) ||
                 p.City.ToLower().Contains(keyword.ToLower()) ||
                 p.Address.ToLower().Contains(keyword.ToLower())))
            .ToListAsync();

    // ---- Haversine Formula ----
    // Calculates distance in KM between two GPS coordinates
    private static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double EarthRadiusKm = 6371.0;
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusKm * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
