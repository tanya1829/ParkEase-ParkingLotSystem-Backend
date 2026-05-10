using ParkEase.SpotService.Entities;

namespace ParkEase.SpotService.Interfaces;

/// <summary>Data access contract for parking spot operations</summary>
public interface ISpotRepository
{
    Task<List<ParkingSpot>> FindByLotIdAsync(int lotId);
    Task<List<ParkingSpot>> FindByLotIdAndStatusAsync(int lotId, string status);
    Task<List<ParkingSpot>> FindByLotIdAndSpotTypeAsync(int lotId, string spotType);
    Task<List<ParkingSpot>> FindByLotIdAndVehicleTypeAsync(int lotId, string vehicleType);
    Task<ParkingSpot?> FindBySpotIdAsync(int spotId);
    Task<int> CountByLotIdAndStatusAsync(int lotId, string status);
    Task<List<ParkingSpot>> FindByIsEVChargingAsync(bool isEV);
    Task<ParkingSpot> CreateAsync(ParkingSpot spot);
    Task<List<ParkingSpot>> CreateBulkAsync(List<ParkingSpot> spots);
    Task<ParkingSpot> UpdateAsync(ParkingSpot spot);
    Task DeleteBySpotIdAsync(int spotId);
    Task<bool> ExistsByLotIdAndSpotNumberAsync(int lotId, string spotNumber);
}
