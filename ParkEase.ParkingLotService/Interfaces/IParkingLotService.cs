using ParkEase.ParkingLotService.DTOs;

namespace ParkEase.ParkingLotService.Interfaces;

public interface IParkingLotService
{
    Task<ApiResponse<ParkingLotDto>> CreateLotAsync(CreateLotRequest request);
    Task<ApiResponse<ParkingLotDto>> GetLotByIdAsync(int lotId);
    Task<ApiResponse<List<ParkingLotDto>>> GetLotsByCityAsync(string city);
    Task<ApiResponse<List<ParkingLotDto>>> GetNearbyLotsAsync(double lat, double lng, double radiusKm);
    Task<ApiResponse<List<ParkingLotDto>>> GetLotsByManagerAsync(int managerId);
    Task<ApiResponse<ParkingLotDto>> UpdateLotAsync(int lotId, UpdateLotRequest request);
    Task<ApiResponse<string>> ToggleOpenAsync(int lotId);
    Task<ApiResponse<string>> ApproveLotAsync(int lotId);
    Task<ApiResponse<string>> RejectLotAsync(int lotId);
    Task<ApiResponse<string>> DeleteLotAsync(int lotId);
    Task<ApiResponse<string>> DecrementAvailableAsync(int lotId);
    Task<ApiResponse<string>> IncrementAvailableAsync(int lotId);
    Task<ApiResponse<List<ParkingLotDto>>> SearchLotsAsync(string keyword);
    Task<ApiResponse<List<ParkingLotDto>>> GetPendingApprovalAsync();
    Task<ApiResponse<List<ParkingLotDto>>> GetAllLotsAsync();
}
