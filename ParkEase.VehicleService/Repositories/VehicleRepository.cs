using Microsoft.EntityFrameworkCore;
using ParkEase.VehicleService.Data;
using ParkEase.VehicleService.Entities;
using ParkEase.VehicleService.Interfaces;

namespace ParkEase.VehicleService.Repositories;

/// <summary>EF Core implementation of vehicle data access</summary>
public class VehicleRepository : IVehicleRepository
{
    private readonly VehicleDbContext _context;

    public VehicleRepository(VehicleDbContext context)
    {
        _context = context;
    }

    public async Task<List<Vehicle>> FindByOwnerIdAsync(int ownerId) =>
        await _context.Vehicles
            .Where(v => v.OwnerId == ownerId && v.IsActive)
            .OrderBy(v => v.RegisteredAt)
            .ToListAsync();

    public async Task<Vehicle?> FindByLicensePlateAsync(string licensePlate) =>
        await _context.Vehicles
            .FirstOrDefaultAsync(v => v.LicensePlate.ToLower() == licensePlate.ToLower());

    public async Task<Vehicle?> FindByVehicleIdAsync(int vehicleId) =>
        await _context.Vehicles.FindAsync(vehicleId);

    public async Task<List<Vehicle>> FindByVehicleTypeAsync(string vehicleType) =>
        await _context.Vehicles
            .Where(v => v.VehicleType == vehicleType.ToUpper() && v.IsActive)
            .ToListAsync();

    public async Task<List<Vehicle>> FindByIsEVAsync(bool isEV) =>
        await _context.Vehicles
            .Where(v => v.IsEV == isEV && v.IsActive)
            .ToListAsync();

    public async Task<bool> ExistsByLicensePlateAsync(string licensePlate, int ownerId) =>
        await _context.Vehicles
            .AnyAsync(v => v.LicensePlate.ToLower() == licensePlate.ToLower()
                        && v.OwnerId == ownerId);

    public async Task<Vehicle> CreateAsync(Vehicle vehicle)
    {
        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();
        return vehicle;
    }

    public async Task<Vehicle> UpdateAsync(Vehicle vehicle)
    {
        vehicle.UpdatedAt = DateTime.UtcNow;
        _context.Vehicles.Update(vehicle);
        await _context.SaveChangesAsync();
        return vehicle;
    }

    public async Task DeleteByVehicleIdAsync(int vehicleId)
    {
        var vehicle = await _context.Vehicles.FindAsync(vehicleId);
        if (vehicle != null)
        {
            // Soft delete — mark as inactive instead of removing
            vehicle.IsActive = false;
            vehicle.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<Vehicle>> GetAllAsync() =>
        await _context.Vehicles.ToListAsync();
}
