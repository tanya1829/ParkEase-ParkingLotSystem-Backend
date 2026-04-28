namespace ParkEase.ParkingLotService.DTOs;

// ---------- Request DTOs ----------

public class CreateLotRequest
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int TotalSpots { get; set; }
    public int ManagerId { get; set; }
    public string OpenTime { get; set; } = "08:00";   // HH:mm format
    public string CloseTime { get; set; } = "22:00";  // HH:mm format
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
}

public class UpdateLotRequest
{
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? OpenTime { get; set; }
    public string? CloseTime { get; set; }
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
}

public class NearbyLotsRequest
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double RadiusKm { get; set; } = 5.0;  // default 5km
}

// ---------- Response DTOs ----------

public class ParkingLotDto
{
    public int LotId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int TotalSpots { get; set; }
    public int AvailableSpots { get; set; }
    public int ManagerId { get; set; }
    public bool IsOpen { get; set; }
    public bool IsApproved { get; set; }
    public string OpenTime { get; set; } = string.Empty;
    public string CloseTime { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public double? DistanceKm { get; set; }  // filled for nearby searches
}

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
