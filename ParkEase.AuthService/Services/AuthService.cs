using Microsoft.EntityFrameworkCore;
using ParkEase.AuthService.Data;
using ParkEase.AuthService.DTOs;
using ParkEase.AuthService.Entities;
using ParkEase.AuthService.Helpers;
using ParkEase.AuthService.Interfaces;

namespace ParkEase.AuthService.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly AuthDbContext _context;
    private readonly JwtHelper _jwtHelper;

    public AuthService(IUserRepository userRepo, AuthDbContext context, JwtHelper jwtHelper)
    {
        _userRepo = userRepo;
        _context = context;
        _jwtHelper = jwtHelper;
    }

    public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        if (await _userRepo.ExistsByEmailAsync(request.Email))
            return ApiResponse<AuthResponse>.Fail("Email already registered.");

        var validRoles = new[] { "DRIVER", "MANAGER" };
        if (!validRoles.Contains(request.Role.ToUpper()))
            return ApiResponse<AuthResponse>.Fail("Invalid role. Use DRIVER or MANAGER.");

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Phone = request.Phone,
            Role = request.Role.ToUpper(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _userRepo.CreateAsync(user);
        return await BuildAuthResponseAsync(created, "Registration successful.");
    }

    public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request)
    {
        var user = await _userRepo.FindByEmailAsync(request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return ApiResponse<AuthResponse>.Fail("Invalid email or password.");

        if (!user.IsActive)
            return ApiResponse<AuthResponse>.Fail("Account is deactivated. Contact support.");

        return await BuildAuthResponseAsync(user, "Login successful.");
    }

    public async Task<ApiResponse<string>> LogoutAsync(string token)
    {
        // Revoke the refresh token associated with this session
        // In a full implementation, maintain a token blacklist or just rely on expiry
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(r => !r.IsRevoked && r.ExpiresAt > DateTime.UtcNow);

        if (refreshToken != null)
        {
            refreshToken.IsRevoked = true;
            await _context.SaveChangesAsync();
        }

        return ApiResponse<string>.Ok("Logged out successfully.");
    }

    public async Task<ApiResponse<AuthResponse>> RefreshTokenAsync(string refreshToken)
    {
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == refreshToken && !r.IsRevoked);

        if (storedToken == null || storedToken.ExpiresAt < DateTime.UtcNow)
            return ApiResponse<AuthResponse>.Fail("Invalid or expired refresh token.");

        var user = await _userRepo.FindByUserIdAsync(storedToken.UserId);
        if (user == null || !user.IsActive)
            return ApiResponse<AuthResponse>.Fail("User not found or deactivated.");

        // Revoke old refresh token
        storedToken.IsRevoked = true;
        await _context.SaveChangesAsync();

        return await BuildAuthResponseAsync(user, "Token refreshed.");
    }

    public async Task<ApiResponse<UserDto>> GetProfileAsync(int userId)
    {
        var user = await _userRepo.FindByUserIdAsync(userId);
        if (user == null) return ApiResponse<UserDto>.Fail("User not found.");

        return ApiResponse<UserDto>.Ok(MapToDto(user));
    }

    public async Task<ApiResponse<UserDto>> UpdateProfileAsync(int userId, UpdateProfileRequest request)
    {
        var user = await _userRepo.FindByUserIdAsync(userId);
        if (user == null) return ApiResponse<UserDto>.Fail("User not found.");

        if (request.FullName != null) user.FullName = request.FullName;
        if (request.Phone != null) user.Phone = request.Phone;
        if (request.ProfilePicUrl != null) user.ProfilePicUrl = request.ProfilePicUrl;
        if (request.VehiclePlate != null) user.VehiclePlate = request.VehiclePlate;

        var updated = await _userRepo.UpdateAsync(user);
        return ApiResponse<UserDto>.Ok(MapToDto(updated), "Profile updated.");
    }

    public async Task<ApiResponse<string>> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var user = await _userRepo.FindByUserIdAsync(userId);
        if (user == null) return ApiResponse<string>.Fail("User not found.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return ApiResponse<string>.Fail("Current password is incorrect.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _userRepo.UpdateAsync(user);

        return ApiResponse<string>.Ok("Password changed successfully.");
    }

    public async Task<ApiResponse<string>> DeactivateAccountAsync(int userId)
    {
        var user = await _userRepo.FindByUserIdAsync(userId);
        if (user == null) return ApiResponse<string>.Fail("User not found.");

        user.IsActive = false;
        await _userRepo.UpdateAsync(user);

        return ApiResponse<string>.Ok("Account deactivated.");
    }

    public async Task<ApiResponse<AuthResponse>> OAuthLoginAsync(string provider, string providerId,
        string email, string fullName)
    {
        // Check if OAuth user already exists
        var existingOAuth = await _userRepo.FindByOAuthAsync(provider, providerId);
        if (existingOAuth != null)
            return await BuildAuthResponseAsync(existingOAuth, "OAuth login successful.");

        // Check if email already registered (link accounts)
        var existingEmail = await _userRepo.FindByEmailAsync(email);
        if (existingEmail != null)
        {
            existingEmail.OAuthProvider = provider;
            existingEmail.OAuthProviderId = providerId;
            await _userRepo.UpdateAsync(existingEmail);
            return await BuildAuthResponseAsync(existingEmail, "Account linked and logged in.");
        }

        // New OAuth user — register automatically as DRIVER
        var newUser = new User
        {
            FullName = fullName,
            Email = email.ToLower(),
            PasswordHash = string.Empty, // no password for OAuth users
            Role = "DRIVER",
            IsActive = true,
            OAuthProvider = provider,
            OAuthProviderId = providerId,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _userRepo.CreateAsync(newUser);
        return await BuildAuthResponseAsync(created, "OAuth registration and login successful.");
    }

    public bool ValidateToken(string token) => _jwtHelper.ValidateToken(token);

    // ---- Private Helpers ----

    private async Task<ApiResponse<AuthResponse>> BuildAuthResponseAsync(User user, string message)
    {
        var accessToken = _jwtHelper.GenerateAccessToken(user);
        var refreshTokenStr = _jwtHelper.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            UserId = user.UserId,
            Token = refreshTokenStr,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return ApiResponse<AuthResponse>.Ok(new AuthResponse
        {
            Token = accessToken,
            RefreshToken = refreshTokenStr,
            User = MapToDto(user)
        }, message);
    }

    private static UserDto MapToDto(User u) => new()
    {
        UserId = u.UserId,
        FullName = u.FullName,
        Email = u.Email,
        Phone = u.Phone,
        Role = u.Role,
        IsActive = u.IsActive,
        ProfilePicUrl = u.ProfilePicUrl,
        VehiclePlate = u.VehiclePlate,
        CreatedAt = u.CreatedAt
    };
}
