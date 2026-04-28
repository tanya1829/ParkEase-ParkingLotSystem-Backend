using ParkEase.SpotService.DTOs;

namespace ParkEase.SpotService.Interfaces;

/// <summary>Business logic contract for parking spot operations</summary>
public interface ISpotService
{
    Task<ApiResponse<ParkingSpotDto>> AddSpotAsync(AddSpotRequest request);
    Task<ApiResponse<List<ParkingSpotDto>>> AddBulkSpotsAsync(AddBulkSpotsRequest request);
    Task<ApiResponse<ParkingSpotDto>> GetSpotByIdAsync(int spotId);
    Task<ApiResponse<List<ParkingSpotDto>>> GetSpotsByLotAsync(int lotId);
    Task<ApiResponse<List<ParkingSpotDto>>> GetAvailableSpotsByLotAsync(int lotId);
    Task<ApiResponse<List<ParkingSpotDto>>> GetByTypeAndLotAsync(int lotId, string spotType);
    Task<ApiResponse<List<ParkingSpotDto>>> GetByVehicleTypeAsync(int lotId, string vehicleType);
    Task<ApiResponse<ParkingSpotDto>> OccupySpotAsync(int spotId);
    Task<ApiResponse<ParkingSpotDto>> ReserveSpotAsync(int spotId);
    Task<ApiResponse<ParkingSpotDto>> ReleaseSpotAsync(int spotId);
    Task<ApiResponse<ParkingSpotDto>> UpdateSpotAsync(int spotId, UpdateSpotRequest request);
    Task<ApiResponse<string>> DeleteSpotAsync(int spotId);
    Task<ApiResponse<int>> CountAvailableAsync(int lotId);
}
