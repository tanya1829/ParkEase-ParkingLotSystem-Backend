using ParkEase.ParkingLotService.DTOs;
using ParkEase.ParkingLotService.Entities;
using ParkEase.ParkingLotService.Interfaces;

namespace ParkEase.ParkingLotService.Services;

public class ParkingLotService : IParkingLotService
{
    private readonly IParkingLotRepository _repo;

    public ParkingLotService(IParkingLotRepository repo)
    {
        _repo = repo;
    }

    public async Task<ApiResponse<ParkingLotDto>> CreateLotAsync(CreateLotRequest request)
    {
        if (request.TotalSpots <= 0)
            return ApiResponse<ParkingLotDto>.Fail("Total spots must be greater than 0.");

        if (!TimeOnly.TryParse(request.OpenTime, out var openTime))
            return ApiResponse<ParkingLotDto>.Fail("Invalid OpenTime format. Use HH:mm.");

        if (!TimeOnly.TryParse(request.CloseTime, out var closeTime))
            return ApiResponse<ParkingLotDto>.Fail("Invalid CloseTime format. Use HH:mm.");

        var lot = new ParkingLot
        {
            Name = request.Name,
            Address = request.Address,
            City = request.City,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            TotalSpots = request.TotalSpots,
            AvailableSpots = request.TotalSpots,  // initially all spots available
            ManagerId = request.ManagerId,
            OpenTime = openTime,
            CloseTime = closeTime,
            ImageUrl = request.ImageUrl,
            Description = request.Description,
            IsOpen = false,      // closed until admin approves
            IsApproved = false,  // pending admin approval
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repo.CreateAsync(lot);
        return ApiResponse<ParkingLotDto>.Ok(MapToDto(created),
            "Parking lot registered successfully. Awaiting admin approval.");
    }

    public async Task<ApiResponse<ParkingLotDto>> GetLotByIdAsync(int lotId)
    {
        var lot = await _repo.FindByLotIdAsync(lotId);
        if (lot == null) return ApiResponse<ParkingLotDto>.Fail("Parking lot not found.");
        return ApiResponse<ParkingLotDto>.Ok(MapToDto(lot));
    }

    public async Task<ApiResponse<List<ParkingLotDto>>> GetLotsByCityAsync(string city)
    {
        var lots = await _repo.FindByCityAsync(city);
        return ApiResponse<List<ParkingLotDto>>.Ok(lots.Select(MapToDto).ToList());
    }

    public async Task<ApiResponse<List<ParkingLotDto>>> GetNearbyLotsAsync(
        double lat, double lng, double radiusKm)
    {
        var lots = await _repo.FindNearbyAsync(lat, lng, radiusKm);
        var dtos = lots.Select(lot =>
        {
            var dto = MapToDto(lot);
            dto.DistanceKm = Math.Round(CalculateDistanceKm(lat, lng, lot.Latitude, lot.Longitude), 2);
            return dto;
        }).ToList();

        return ApiResponse<List<ParkingLotDto>>.Ok(dtos,
            $"Found {dtos.Count} lots within {radiusKm}km.");
    }

    public async Task<ApiResponse<List<ParkingLotDto>>> GetLotsByManagerAsync(int managerId)
    {
        var lots = await _repo.FindByManagerIdAsync(managerId);
        return ApiResponse<List<ParkingLotDto>>.Ok(lots.Select(MapToDto).ToList());
    }

    public async Task<ApiResponse<ParkingLotDto>> UpdateLotAsync(int lotId, UpdateLotRequest request)
    {
        var lot = await _repo.FindByLotIdAsync(lotId);
        if (lot == null) return ApiResponse<ParkingLotDto>.Fail("Parking lot not found.");

        if (request.Name != null) lot.Name = request.Name;
        if (request.Address != null) lot.Address = request.Address;
        if (request.City != null) lot.City = request.City;
        if (request.Latitude.HasValue) lot.Latitude = request.Latitude.Value;
        if (request.Longitude.HasValue) lot.Longitude = request.Longitude.Value;
        if (request.ImageUrl != null) lot.ImageUrl = request.ImageUrl;
        if (request.Description != null) lot.Description = request.Description;

        if (request.OpenTime != null && TimeOnly.TryParse(request.OpenTime, out var openTime))
            lot.OpenTime = openTime;

        if (request.CloseTime != null && TimeOnly.TryParse(request.CloseTime, out var closeTime))
            lot.CloseTime = closeTime;

        var updated = await _repo.UpdateAsync(lot);
        return ApiResponse<ParkingLotDto>.Ok(MapToDto(updated), "Lot updated successfully.");
    }

    public async Task<ApiResponse<string>> ToggleOpenAsync(int lotId)
    {
        var lot = await _repo.FindByLotIdAsync(lotId);
        if (lot == null) return ApiResponse<string>.Fail("Parking lot not found.");

        if (!lot.IsApproved)
            return ApiResponse<string>.Fail("Lot must be approved by admin before opening.");

        lot.IsOpen = !lot.IsOpen;
        await _repo.UpdateAsync(lot);

        var status = lot.IsOpen ? "opened" : "closed";
        return ApiResponse<string>.Ok($"Lot has been {status} successfully.");
    }

    public async Task<ApiResponse<string>> ApproveLotAsync(int lotId)
    {
        var lot = await _repo.FindByLotIdAsync(lotId);
        if (lot == null) return ApiResponse<string>.Fail("Parking lot not found.");

        lot.IsApproved = true;
        await _repo.UpdateAsync(lot);
        return ApiResponse<string>.Ok("Lot approved successfully. Manager can now open it.");
    }

    public async Task<ApiResponse<string>> RejectLotAsync(int lotId)
    {
        var lot = await _repo.FindByLotIdAsync(lotId);
        if (lot == null) return ApiResponse<string>.Fail("Parking lot not found.");

        await _repo.DeleteByLotIdAsync(lotId);
        return ApiResponse<string>.Ok("Lot registration rejected and removed.");
    }

    public async Task<ApiResponse<string>> DeleteLotAsync(int lotId)
    {
        var lot = await _repo.FindByLotIdAsync(lotId);
        if (lot == null) return ApiResponse<string>.Fail("Parking lot not found.");

        await _repo.DeleteByLotIdAsync(lotId);
        return ApiResponse<string>.Ok("Lot deleted successfully.");
    }

    public async Task<ApiResponse<string>> DecrementAvailableAsync(int lotId)
    {
        var lot = await _repo.FindByLotIdAsync(lotId);
        if (lot == null) return ApiResponse<string>.Fail("Parking lot not found.");

        if (lot.AvailableSpots <= 0)
            return ApiResponse<string>.Fail("No available spots remaining.");

        lot.AvailableSpots--;
        await _repo.UpdateAsync(lot);
        return ApiResponse<string>.Ok($"Available spots decremented. Remaining: {lot.AvailableSpots}");
    }

    public async Task<ApiResponse<string>> IncrementAvailableAsync(int lotId)
    {
        var lot = await _repo.FindByLotIdAsync(lotId);
        if (lot == null) return ApiResponse<string>.Fail("Parking lot not found.");

        if (lot.AvailableSpots >= lot.TotalSpots)
            return ApiResponse<string>.Fail("Available spots already at maximum.");

        lot.AvailableSpots++;
        await _repo.UpdateAsync(lot);
        return ApiResponse<string>.Ok($"Available spots incremented. Current: {lot.AvailableSpots}");
    }

    public async Task<ApiResponse<List<ParkingLotDto>>> SearchLotsAsync(string keyword)
    {
        var lots = await _repo.SearchAsync(keyword);
        return ApiResponse<List<ParkingLotDto>>.Ok(lots.Select(MapToDto).ToList(),
            $"Found {lots.Count} lots matching '{keyword}'.");
    }

    public async Task<ApiResponse<List<ParkingLotDto>>> GetPendingApprovalAsync()
    {
        var lots = await _repo.FindPendingApprovalAsync();
        return ApiResponse<List<ParkingLotDto>>.Ok(lots.Select(MapToDto).ToList());
    }

    public async Task<ApiResponse<List<ParkingLotDto>>> GetAllLotsAsync()
    {
        var lots = await _repo.GetAllAsync();
        return ApiResponse<List<ParkingLotDto>>.Ok(lots.Select(MapToDto).ToList());
    }

    // ---- Private Helpers ----

    private static ParkingLotDto MapToDto(ParkingLot lot) => new()
    {
        LotId = lot.LotId,
        Name = lot.Name,
        Address = lot.Address,
        City = lot.City,
        Latitude = lot.Latitude,
        Longitude = lot.Longitude,
        TotalSpots = lot.TotalSpots,
        AvailableSpots = lot.AvailableSpots,
        ManagerId = lot.ManagerId,
        IsOpen = lot.IsOpen,
        IsApproved = lot.IsApproved,
        OpenTime = lot.OpenTime.ToString("HH:mm"),
        CloseTime = lot.CloseTime.ToString("HH:mm"),
        ImageUrl = lot.ImageUrl,
        Description = lot.Description,
        CreatedAt = lot.CreatedAt
    };

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
