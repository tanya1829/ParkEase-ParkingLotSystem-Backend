using ParkEase.VehicleService.DTOs;

namespace ParkEase.VehicleService.Interfaces;

/// <summary>Business logic contract for vehicle operations</summary>
public interface IVehicleService
{
    Task<ApiResponse<VehicleDto>> RegisterVehicleAsync(RegisterVehicleRequest request);
    Task<ApiResponse<VehicleDto>> GetVehicleByIdAsync(int vehicleId);
    Task<ApiResponse<List<VehicleDto>>> GetVehiclesByOwnerAsync(int ownerId);
    Task<ApiResponse<VehicleDto>> GetByLicensePlateAsync(string licensePlate);
    Task<ApiResponse<VehicleDto>> UpdateVehicleAsync(int vehicleId, UpdateVehicleRequest request);
    Task<ApiResponse<string>> DeleteVehicleAsync(int vehicleId);
    Task<ApiResponse<string>> GetVehicleTypeAsync(int vehicleId);
    Task<ApiResponse<bool>> IsEVVehicleAsync(int vehicleId);
    Task<ApiResponse<List<VehicleDto>>> GetAllVehiclesAsync();
}
