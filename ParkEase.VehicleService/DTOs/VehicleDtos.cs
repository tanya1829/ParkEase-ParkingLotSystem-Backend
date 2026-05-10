namespace ParkEase.VehicleService.DTOs;

// ---------- Request DTOs ----------

/// <summary>Request to register a new vehicle</summary>
public class RegisterVehicleRequest
{
    public int OwnerId { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string VehicleType { get; set; } = "4W";  // 2W | 4W | HEAVY
    public bool IsEV { get; set; } = false;
}

/// <summary>Request to update vehicle details</summary>
public class UpdateVehicleRequest
{
    public string? Make { get; set; }
    public string? Model { get; set; }
    public string? Color { get; set; }
    public string? VehicleType { get; set; }
    public bool? IsEV { get; set; }
}

// ---------- Response DTOs ----------

/// <summary>Vehicle details returned in API responses</summary>
public class VehicleDto
{
    public int VehicleId { get; set; }
    public int OwnerId { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string VehicleType { get; set; } = string.Empty;
    public bool IsEV { get; set; }
    public bool IsActive { get; set; }
    public DateTime RegisteredAt { get; set; }
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
