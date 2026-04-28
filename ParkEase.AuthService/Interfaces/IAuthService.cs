using ParkEase.AuthService.DTOs;

namespace ParkEase.AuthService.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request);
    Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request);
    Task<ApiResponse<string>> LogoutAsync(string token);
    Task<ApiResponse<AuthResponse>> RefreshTokenAsync(string refreshToken);
    Task<ApiResponse<UserDto>> GetProfileAsync(int userId);
    Task<ApiResponse<UserDto>> UpdateProfileAsync(int userId, UpdateProfileRequest request);
    Task<ApiResponse<string>> ChangePasswordAsync(int userId, ChangePasswordRequest request);
    Task<ApiResponse<string>> DeactivateAccountAsync(int userId);
    Task<ApiResponse<AuthResponse>> OAuthLoginAsync(string provider, string providerId,
        string email, string fullName);
    bool ValidateToken(string token);
}
