namespace ParkEase.AuthService.DTOs;

// ---------- Request DTOs ----------

public class RegisterRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Role { get; set; } = "DRIVER";
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UpdateProfileRequest
{
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? ProfilePicUrl { get; set; }
    public string? VehiclePlate { get; set; }
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

// ---------- Response DTOs ----------

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public UserDto User { get; set; } = new();
}

public class UserDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? ProfilePicUrl { get; set; }
    public string? VehiclePlate { get; set; }
    public DateTime CreatedAt { get; set; }
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
