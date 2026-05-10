using Microsoft.EntityFrameworkCore;
using ParkEase.SpotService.Data;
using ParkEase.SpotService.Entities;
using ParkEase.SpotService.Interfaces;

namespace ParkEase.SpotService.Repositories;

/// <summary>EF Core implementation of spot data access operations</summary>
public class SpotRepository : ISpotRepository
{
    private readonly SpotDbContext _context;

    public SpotRepository(SpotDbContext context)
    {
        _context = context;
    }

    public async Task<List<ParkingSpot>> FindByLotIdAsync(int lotId) =>
        await _context.ParkingSpots
            .Where(s => s.LotId == lotId)
            .OrderBy(s => s.Floor)
            .ThenBy(s => s.SpotNumber)
            .ToListAsync();

    public async Task<List<ParkingSpot>> FindByLotIdAndStatusAsync(int lotId, string status) =>
        await _context.ParkingSpots
            .Where(s => s.LotId == lotId && s.Status == status)
            .OrderBy(s => s.Floor)
            .ThenBy(s => s.SpotNumber)
            .ToListAsync();

    public async Task<List<ParkingSpot>> FindByLotIdAndSpotTypeAsync(int lotId, string spotType) =>
        await _context.ParkingSpots
            .Where(s => s.LotId == lotId && s.SpotType == spotType.ToUpper())
            .ToListAsync();

    public async Task<List<ParkingSpot>> FindByLotIdAndVehicleTypeAsync(int lotId, string vehicleType) =>
        await _context.ParkingSpots
            .Where(s => s.LotId == lotId && s.VehicleType == vehicleType.ToUpper())
            .ToListAsync();

    public async Task<ParkingSpot?> FindBySpotIdAsync(int spotId) =>
        await _context.ParkingSpots.FindAsync(spotId);

    public async Task<int> CountByLotIdAndStatusAsync(int lotId, string status) =>
        await _context.ParkingSpots
            .CountAsync(s => s.LotId == lotId && s.Status == status);

    public async Task<List<ParkingSpot>> FindByIsEVChargingAsync(bool isEV) =>
        await _context.ParkingSpots
            .Where(s => s.IsEVCharging == isEV)
            .ToListAsync();

    public async Task<ParkingSpot> CreateAsync(ParkingSpot spot)
    {
        _context.ParkingSpots.Add(spot);
        await _context.SaveChangesAsync();
        return spot;
    }

    public async Task<List<ParkingSpot>> CreateBulkAsync(List<ParkingSpot> spots)
    {
        _context.ParkingSpots.AddRange(spots);
        await _context.SaveChangesAsync();
        return spots;
    }

    public async Task<ParkingSpot> UpdateAsync(ParkingSpot spot)
    {
        spot.UpdatedAt = DateTime.UtcNow;
        _context.ParkingSpots.Update(spot);
        await _context.SaveChangesAsync();
        return spot;
    }

    public async Task DeleteBySpotIdAsync(int spotId)
    {
        var spot = await _context.ParkingSpots.FindAsync(spotId);
        if (spot != null)
        {
            _context.ParkingSpots.Remove(spot);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsByLotIdAndSpotNumberAsync(int lotId, string spotNumber) =>
        await _context.ParkingSpots
            .AnyAsync(s => s.LotId == lotId && s.SpotNumber == spotNumber);
}
