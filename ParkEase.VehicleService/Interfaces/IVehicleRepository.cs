using ParkEase.VehicleService.Entities;

namespace ParkEase.VehicleService.Interfaces;

/// <summary>Data access contract for vehicle operations</summary>
public interface IVehicleRepository
{
    Task<List<Vehicle>> FindByOwnerIdAsync(int ownerId);
    Task<Vehicle?> FindByLicensePlateAsync(string licensePlate);
    Task<Vehicle?> FindByVehicleIdAsync(int vehicleId);
    Task<List<Vehicle>> FindByVehicleTypeAsync(string vehicleType);
    Task<List<Vehicle>> FindByIsEVAsync(bool isEV);
    Task<bool> ExistsByLicensePlateAsync(string licensePlate, int ownerId);
    Task<Vehicle> CreateAsync(Vehicle vehicle);
    Task<Vehicle> UpdateAsync(Vehicle vehicle);
    Task DeleteByVehicleIdAsync(int vehicleId);
    Task<List<Vehicle>> GetAllAsync();
}
