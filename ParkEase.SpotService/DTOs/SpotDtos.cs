namespace ParkEase.SpotService.DTOs;

// ---------- Request DTOs ----------

/// <summary>Request to add a single parking spot to a lot</summary>
public class AddSpotRequest
{
    public int LotId { get; set; }
    public string SpotNumber { get; set; } = string.Empty;
    public int Floor { get; set; } = 1;
    public string SpotType { get; set; } = "STANDARD";   // COMPACT | STANDARD | LARGE | MOTORBIKE | EV
    public string VehicleType { get; set; } = "4W";      // 2W | 4W | HEAVY
    public bool IsHandicapped { get; set; } = false;
    public bool IsEVCharging { get; set; } = false;
    public decimal PricePerHour { get; set; }
}

/// <summary>Request to bulk-create multiple spots for a lot</summary>
public class AddBulkSpotsRequest
{
    public int LotId { get; set; }
    public int Count { get; set; }                       // number of spots to create
    public string SpotNumberPrefix { get; set; } = "A"; // e.g. "A" → A1, A2, A3...
    public int Floor { get; set; } = 1;
    public string SpotType { get; set; } = "STANDARD";
    public string VehicleType { get; set; } = "4W";
    public bool IsHandicapped { get; set; } = false;
    public bool IsEVCharging { get; set; } = false;
    public decimal PricePerHour { get; set; }
}

/// <summary>Request to update spot details</summary>
public class UpdateSpotRequest
{
    public string? SpotType { get; set; }
    public string? VehicleType { get; set; }
    public bool? IsHandicapped { get; set; }
    public bool? IsEVCharging { get; set; }
    public decimal? PricePerHour { get; set; }
}

// ---------- Response DTOs ----------

/// <summary>Parking spot details returned in API responses</summary>
public class ParkingSpotDto
{
    public int SpotId { get; set; }
    public int LotId { get; set; }
    public string SpotNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
    public string SpotType { get; set; } = string.Empty;
    public string VehicleType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsHandicapped { get; set; }
    public bool IsEVCharging { get; set; }
    public decimal PricePerHour { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Generic API response wrapper</summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Success") =>
        new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message) =>
        new() { Success = false, Message = message };
}
