using ParkEase.SpotService.DTOs;
using ParkEase.SpotService.Entities;
using ParkEase.SpotService.Interfaces;

namespace ParkEase.SpotService.Services;

/// <summary>
/// Business logic for parking spot management.
/// Handles spot creation, status transitions, and filtering.
/// </summary>
public class SpotService : ISpotService
{
    private readonly ISpotRepository _repo;

    // Valid values for validation
    private static readonly string[] ValidSpotTypes = { "COMPACT", "STANDARD", "LARGE", "MOTORBIKE", "EV" };
    private static readonly string[] ValidVehicleTypes = { "2W", "4W", "HEAVY" };
    private static readonly string[] ValidStatuses = { "AVAILABLE", "RESERVED", "OCCUPIED" };

    public SpotService(ISpotRepository repo)
    {
        _repo = repo;
    }

    public async Task<ApiResponse<ParkingSpotDto>> AddSpotAsync(AddSpotRequest request)
    {
        // Validate spot type
        if (!ValidSpotTypes.Contains(request.SpotType.ToUpper()))
            return ApiResponse<ParkingSpotDto>.Fail($"Invalid SpotType. Valid values: {string.Join(", ", ValidSpotTypes)}");

        // Validate vehicle type
        if (!ValidVehicleTypes.Contains(request.VehicleType.ToUpper()))
            return ApiResponse<ParkingSpotDto>.Fail($"Invalid VehicleType. Valid values: {string.Join(", ", ValidVehicleTypes)}");

        // Validate price
        if (request.PricePerHour <= 0)
            return ApiResponse<ParkingSpotDto>.Fail("PricePerHour must be greater than 0.");

        // Check duplicate spot number in same lot
        if (await _repo.ExistsByLotIdAndSpotNumberAsync(request.LotId, request.SpotNumber))
            return ApiResponse<ParkingSpotDto>.Fail($"Spot number '{request.SpotNumber}' already exists in this lot.");

        var spot = new ParkingSpot
        {
            LotId = request.LotId,
            SpotNumber = request.SpotNumber,
            Floor = request.Floor,
            SpotType = request.SpotType.ToUpper(),
            VehicleType = request.VehicleType.ToUpper(),
            Status = "AVAILABLE",
            IsHandicapped = request.IsHandicapped,
            IsEVCharging = request.IsEVCharging,
            PricePerHour = request.PricePerHour,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repo.CreateAsync(spot);
        return ApiResponse<ParkingSpotDto>.Ok(MapToDto(created), "Spot added successfully.");
    }

    public async Task<ApiResponse<List<ParkingSpotDto>>> AddBulkSpotsAsync(AddBulkSpotsRequest request)
    {
        if (request.Count <= 0 || request.Count > 500)
            return ApiResponse<List<ParkingSpotDto>>.Fail("Count must be between 1 and 500.");

        if (request.PricePerHour <= 0)
            return ApiResponse<List<ParkingSpotDto>>.Fail("PricePerHour must be greater than 0.");

        var spots = new List<ParkingSpot>();

        for (int i = 1; i <= request.Count; i++)
        {
            var spotNumber = $"{request.SpotNumberPrefix}{i}";

            // Skip if spot number already exists
            if (await _repo.ExistsByLotIdAndSpotNumberAsync(request.LotId, spotNumber))
                continue;

            spots.Add(new ParkingSpot
            {
                LotId = request.LotId,
                SpotNumber = spotNumber,
                Floor = request.Floor,
                SpotType = request.SpotType.ToUpper(),
                VehicleType = request.VehicleType.ToUpper(),
                Status = "AVAILABLE",
                IsHandicapped = request.IsHandicapped,
                IsEVCharging = request.IsEVCharging,
                PricePerHour = request.PricePerHour,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (spots.Count == 0)
            return ApiResponse<List<ParkingSpotDto>>.Fail("All spot numbers already exist in this lot.");

        var created = await _repo.CreateBulkAsync(spots);
        return ApiResponse<List<ParkingSpotDto>>.Ok(
            created.Select(MapToDto).ToList(),
            $"{created.Count} spots created successfully.");
    }

    public async Task<ApiResponse<ParkingSpotDto>> GetSpotByIdAsync(int spotId)
    {
        var spot = await _repo.FindBySpotIdAsync(spotId);
        if (spot == null) return ApiResponse<ParkingSpotDto>.Fail("Spot not found.");
        return ApiResponse<ParkingSpotDto>.Ok(MapToDto(spot));
    }

    public async Task<ApiResponse<List<ParkingSpotDto>>> GetSpotsByLotAsync(int lotId)
    {
        var spots = await _repo.FindByLotIdAsync(lotId);
        return ApiResponse<List<ParkingSpotDto>>.Ok(spots.Select(MapToDto).ToList());
    }

    public async Task<ApiResponse<List<ParkingSpotDto>>> GetAvailableSpotsByLotAsync(int lotId)
    {
        var spots = await _repo.FindByLotIdAndStatusAsync(lotId, "AVAILABLE");
        return ApiResponse<List<ParkingSpotDto>>.Ok(
            spots.Select(MapToDto).ToList(),
            $"{spots.Count} available spots found.");
    }

    public async Task<ApiResponse<List<ParkingSpotDto>>> GetByTypeAndLotAsync(int lotId, string spotType)
    {
        var spots = await _repo.FindByLotIdAndSpotTypeAsync(lotId, spotType);
        return ApiResponse<List<ParkingSpotDto>>.Ok(spots.Select(MapToDto).ToList());
    }

    public async Task<ApiResponse<List<ParkingSpotDto>>> GetByVehicleTypeAsync(int lotId, string vehicleType)
    {
        var spots = await _repo.FindByLotIdAndVehicleTypeAsync(lotId, vehicleType);
        return ApiResponse<List<ParkingSpotDto>>.Ok(spots.Select(MapToDto).ToList());
    }

    public async Task<ApiResponse<ParkingSpotDto>> OccupySpotAsync(int spotId)
    {
        var spot = await _repo.FindBySpotIdAsync(spotId);
        if (spot == null) return ApiResponse<ParkingSpotDto>.Fail("Spot not found.");

        if (spot.Status == "OCCUPIED")
            return ApiResponse<ParkingSpotDto>.Fail("Spot is already occupied.");

        // Transition: RESERVED → OCCUPIED (on check-in)
        spot.Status = "OCCUPIED";
        var updated = await _repo.UpdateAsync(spot);
        return ApiResponse<ParkingSpotDto>.Ok(MapToDto(updated), "Spot marked as occupied.");
    }

    public async Task<ApiResponse<ParkingSpotDto>> ReserveSpotAsync(int spotId)
    {
        var spot = await _repo.FindBySpotIdAsync(spotId);
        if (spot == null) return ApiResponse<ParkingSpotDto>.Fail("Spot not found.");

        if (spot.Status != "AVAILABLE")
            return ApiResponse<ParkingSpotDto>.Fail($"Spot is not available. Current status: {spot.Status}");

        // Transition: AVAILABLE → RESERVED (on booking)
        spot.Status = "RESERVED";
        var updated = await _repo.UpdateAsync(spot);
        return ApiResponse<ParkingSpotDto>.Ok(MapToDto(updated), "Spot reserved successfully.");
    }

    public async Task<ApiResponse<ParkingSpotDto>> ReleaseSpotAsync(int spotId)
    {
        var spot = await _repo.FindBySpotIdAsync(spotId);
        if (spot == null) return ApiResponse<ParkingSpotDto>.Fail("Spot not found.");

        // Transition: RESERVED/OCCUPIED → AVAILABLE (on checkout or cancellation)
        spot.Status = "AVAILABLE";
        var updated = await _repo.UpdateAsync(spot);
        return ApiResponse<ParkingSpotDto>.Ok(MapToDto(updated), "Spot released and now available.");
    }

    public async Task<ApiResponse<ParkingSpotDto>> UpdateSpotAsync(int spotId, UpdateSpotRequest request)
    {
        var spot = await _repo.FindBySpotIdAsync(spotId);
        if (spot == null) return ApiResponse<ParkingSpotDto>.Fail("Spot not found.");

        if (request.SpotType != null)
        {
            if (!ValidSpotTypes.Contains(request.SpotType.ToUpper()))
                return ApiResponse<ParkingSpotDto>.Fail($"Invalid SpotType. Valid: {string.Join(", ", ValidSpotTypes)}");
            spot.SpotType = request.SpotType.ToUpper();
        }

        if (request.VehicleType != null)
        {
            if (!ValidVehicleTypes.Contains(request.VehicleType.ToUpper()))
                return ApiResponse<ParkingSpotDto>.Fail($"Invalid VehicleType. Valid: {string.Join(", ", ValidVehicleTypes)}");
            spot.VehicleType = request.VehicleType.ToUpper();
        }

        if (request.IsHandicapped.HasValue) spot.IsHandicapped = request.IsHandicapped.Value;
        if (request.IsEVCharging.HasValue) spot.IsEVCharging = request.IsEVCharging.Value;
        if (request.PricePerHour.HasValue)
        {
            if (request.PricePerHour.Value <= 0)
                return ApiResponse<ParkingSpotDto>.Fail("PricePerHour must be greater than 0.");
            spot.PricePerHour = request.PricePerHour.Value;
        }

        var updated = await _repo.UpdateAsync(spot);
        return ApiResponse<ParkingSpotDto>.Ok(MapToDto(updated), "Spot updated successfully.");
    }

    public async Task<ApiResponse<string>> DeleteSpotAsync(int spotId)
    {
        var spot = await _repo.FindBySpotIdAsync(spotId);
        if (spot == null) return ApiResponse<string>.Fail("Spot not found.");

        if (spot.Status != "AVAILABLE")
            return ApiResponse<string>.Fail("Cannot delete a spot that is currently reserved or occupied.");

        await _repo.DeleteBySpotIdAsync(spotId);
        return ApiResponse<string>.Ok("Spot deleted successfully.");
    }

    public async Task<ApiResponse<int>> CountAvailableAsync(int lotId)
    {
        var count = await _repo.CountByLotIdAndStatusAsync(lotId, "AVAILABLE");
        return ApiResponse<int>.Ok(count, $"{count} available spots in lot {lotId}.");
    }

    // ---- Private Helper ----
    private static ParkingSpotDto MapToDto(ParkingSpot s) => new()
    {
        SpotId = s.SpotId,
        LotId = s.LotId,
        SpotNumber = s.SpotNumber,
        Floor = s.Floor,
        SpotType = s.SpotType,
        VehicleType = s.VehicleType,
        Status = s.Status,
        IsHandicapped = s.IsHandicapped,
        IsEVCharging = s.IsEVCharging,
        PricePerHour = s.PricePerHour,
        CreatedAt = s.CreatedAt
    };
}
