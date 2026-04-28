using ParkEase.VehicleService.DTOs;
using ParkEase.VehicleService.Entities;
using ParkEase.VehicleService.Interfaces;

namespace ParkEase.VehicleService.Services;

/// <summary>
/// Business logic for vehicle management.
/// Drivers can register multiple vehicles and select one at booking time.
/// </summary>
public class VehicleService : IVehicleService
{
    private readonly IVehicleRepository _repo;

    private static readonly string[] ValidVehicleTypes = { "2W", "4W", "HEAVY" };

    public VehicleService(IVehicleRepository repo)
    {
        _repo = repo;
    }

    public async Task<ApiResponse<VehicleDto>> RegisterVehicleAsync(RegisterVehicleRequest request)
    {
        // Validate vehicle type
        if (!ValidVehicleTypes.Contains(request.VehicleType.ToUpper()))
            return ApiResponse<VehicleDto>.Fail($"Invalid VehicleType. Valid values: {string.Join(", ", ValidVehicleTypes)}");

        // Validate required fields
        if (string.IsNullOrWhiteSpace(request.LicensePlate))
            return ApiResponse<VehicleDto>.Fail("License plate is required.");

        if (string.IsNullOrWhiteSpace(request.Make))
            return ApiResponse<VehicleDto>.Fail("Vehicle make is required.");

        if (string.IsNullOrWhiteSpace(request.Model))
            return ApiResponse<VehicleDto>.Fail("Vehicle model is required.");

        // Check duplicate license plate for same owner
        if (await _repo.ExistsByLicensePlateAsync(request.LicensePlate, request.OwnerId))
            return ApiResponse<VehicleDto>.Fail($"Vehicle with plate '{request.LicensePlate}' already registered.");

        var vehicle = new Vehicle
        {
            OwnerId = request.OwnerId,
            LicensePlate = request.LicensePlate.ToUpper(),
            Make = request.Make,
            Model = request.Model,
            Color = request.Color,
            VehicleType = request.VehicleType.ToUpper(),
            IsEV = request.IsEV,
            IsActive = true,
            RegisteredAt = DateTime.UtcNow
        };

        var created = await _repo.CreateAsync(vehicle);
        return ApiResponse<VehicleDto>.Ok(MapToDto(created), "Vehicle registered successfully.");
    }

    public async Task<ApiResponse<VehicleDto>> GetVehicleByIdAsync(int vehicleId)
    {
        var vehicle = await _repo.FindByVehicleIdAsync(vehicleId);
        if (vehicle == null) return ApiResponse<VehicleDto>.Fail("Vehicle not found.");
        return ApiResponse<VehicleDto>.Ok(MapToDto(vehicle));
    }

    public async Task<ApiResponse<List<VehicleDto>>> GetVehiclesByOwnerAsync(int ownerId)
    {
        var vehicles = await _repo.FindByOwnerIdAsync(ownerId);
        return ApiResponse<List<VehicleDto>>.Ok(
            vehicles.Select(MapToDto).ToList(),
            $"{vehicles.Count} vehicles found.");
    }

    public async Task<ApiResponse<VehicleDto>> GetByLicensePlateAsync(string licensePlate)
    {
        var vehicle = await _repo.FindByLicensePlateAsync(licensePlate);
        if (vehicle == null) return ApiResponse<VehicleDto>.Fail("Vehicle not found.");
        return ApiResponse<VehicleDto>.Ok(MapToDto(vehicle));
    }

    public async Task<ApiResponse<VehicleDto>> UpdateVehicleAsync(int vehicleId, UpdateVehicleRequest request)
    {
        var vehicle = await _repo.FindByVehicleIdAsync(vehicleId);
        if (vehicle == null) return ApiResponse<VehicleDto>.Fail("Vehicle not found.");

        if (request.VehicleType != null)
        {
            if (!ValidVehicleTypes.Contains(request.VehicleType.ToUpper()))
                return ApiResponse<VehicleDto>.Fail($"Invalid VehicleType. Valid: {string.Join(", ", ValidVehicleTypes)}");
            vehicle.VehicleType = request.VehicleType.ToUpper();
        }

        if (request.Make != null) vehicle.Make = request.Make;
        if (request.Model != null) vehicle.Model = request.Model;
        if (request.Color != null) vehicle.Color = request.Color;
        if (request.IsEV.HasValue) vehicle.IsEV = request.IsEV.Value;

        var updated = await _repo.UpdateAsync(vehicle);
        return ApiResponse<VehicleDto>.Ok(MapToDto(updated), "Vehicle updated successfully.");
    }

    public async Task<ApiResponse<string>> DeleteVehicleAsync(int vehicleId)
    {
        var vehicle = await _repo.FindByVehicleIdAsync(vehicleId);
        if (vehicle == null) return ApiResponse<string>.Fail("Vehicle not found.");

        // Soft delete
        await _repo.DeleteByVehicleIdAsync(vehicleId);
        return ApiResponse<string>.Ok("Vehicle removed successfully.");
    }

    public async Task<ApiResponse<string>> GetVehicleTypeAsync(int vehicleId)
    {
        var vehicle = await _repo.FindByVehicleIdAsync(vehicleId);
        if (vehicle == null) return ApiResponse<string>.Fail("Vehicle not found.");
        return ApiResponse<string>.Ok(vehicle.VehicleType);
    }

    public async Task<ApiResponse<bool>> IsEVVehicleAsync(int vehicleId)
    {
        var vehicle = await _repo.FindByVehicleIdAsync(vehicleId);
        if (vehicle == null) return ApiResponse<bool>.Fail("Vehicle not found.");
        return ApiResponse<bool>.Ok(vehicle.IsEV,
            vehicle.IsEV ? "Vehicle is an EV." : "Vehicle is not an EV.");
    }

    public async Task<ApiResponse<List<VehicleDto>>> GetAllVehiclesAsync()
    {
        var vehicles = await _repo.GetAllAsync();
        return ApiResponse<List<VehicleDto>>.Ok(vehicles.Select(MapToDto).ToList());
    }

    // ---- Private Helper ----
    private static VehicleDto MapToDto(Vehicle v) => new()
    {
        VehicleId = v.VehicleId,
        OwnerId = v.OwnerId,
        LicensePlate = v.LicensePlate,
        Make = v.Make,
        Model = v.Model,
        Color = v.Color,
        VehicleType = v.VehicleType,
        IsEV = v.IsEV,
        IsActive = v.IsActive,
        RegisteredAt = v.RegisteredAt
    };
}
