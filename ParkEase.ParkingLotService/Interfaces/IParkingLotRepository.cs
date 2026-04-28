using ParkEase.ParkingLotService.Entities;

namespace ParkEase.ParkingLotService.Interfaces;

public interface IParkingLotRepository
{
    Task<ParkingLot?> FindByLotIdAsync(int lotId);
    Task<List<ParkingLot>> FindByCityAsync(string city);
    Task<List<ParkingLot>> FindByManagerIdAsync(int managerId);
    Task<List<ParkingLot>> FindByIsOpenAsync(bool isOpen);
    Task<List<ParkingLot>> FindNearbyAsync(double lat, double lng, double radiusKm);
    Task<List<ParkingLot>> FindByAvailableSpotsGreaterThanAsync(int minSpots);
    Task<int> CountByCityAsync(string city);
    Task<List<ParkingLot>> FindPendingApprovalAsync();
    Task<List<ParkingLot>> GetAllAsync();
    Task<ParkingLot> CreateAsync(ParkingLot lot);
    Task<ParkingLot> UpdateAsync(ParkingLot lot);
    Task DeleteByLotIdAsync(int lotId);
    Task<List<ParkingLot>> SearchAsync(string keyword);
}
