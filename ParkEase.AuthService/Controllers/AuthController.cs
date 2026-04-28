using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkEase.AuthService.DTOs;
using ParkEase.AuthService.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace ParkEase.AuthService.Controllers;

/// <summary>
/// Handles user registration, login, JWT tokens, profile, and OAuth.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Register a new user (Driver or Lot Manager)</summary>
    [HttpPost("register")]
    [SwaggerOperation(
        Summary = "Register a new user",
        Description = "Creates a new account. Role must be DRIVER or MANAGER. Returns a JWT token on success."
    )]
    [SwaggerResponse(200, "Registration successful — returns JWT token + user details")]
    [SwaggerResponse(400, "Email already registered or invalid role")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Login with email and password</summary>
    [HttpPost("login")]
    [SwaggerOperation(
        Summary = "Login with email and password",
        Description = "Authenticates user credentials and returns a JWT access token (24hr) and refresh token (7 days)."
    )]
    [SwaggerResponse(200, "Login successful — returns JWT token + refresh token")]
    [SwaggerResponse(401, "Invalid email or password")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    /// <summary>Logout and revoke the refresh token</summary>
    [HttpPost("logout")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Logout current user",
        Description = "Revokes the current refresh token. Requires a valid JWT Bearer token in the Authorization header."
    )]
    [SwaggerResponse(200, "Logged out successfully")]
    public async Task<IActionResult> Logout()
    {
        var token = HttpContext.Request.Headers["Authorization"]
            .ToString().Replace("Bearer ", "");
        var result = await _authService.LogoutAsync(token);
        return Ok(result);
    }

    /// <summary>Get a new access token using a refresh token</summary>
    [HttpPost("refresh")]
    [SwaggerOperation(
        Summary = "Refresh JWT access token",
        Description = "Exchange a valid refresh token for a new JWT access token. Old refresh token is invalidated."
    )]
    [SwaggerResponse(200, "New JWT token issued")]
    [SwaggerResponse(401, "Invalid or expired refresh token")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    /// <summary>Get the logged-in user's profile</summary>
    [HttpGet("profile")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Get own profile",
        Description = "Returns the full profile of the currently authenticated user."
    )]
    [SwaggerResponse(200, "Profile returned successfully")]
    [SwaggerResponse(404, "User not found")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetCurrentUserId();
        var result = await _authService.GetProfileAsync(userId);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Update profile details (name, phone, profile picture)</summary>
    [HttpPut("profile")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Update profile",
        Description = "Update full name, phone number, profile picture URL, or vehicle plate for the current user."
    )]
    [SwaggerResponse(200, "Profile updated successfully")]
    [SwaggerResponse(400, "Invalid request data")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _authService.UpdateProfileAsync(userId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Change account password</summary>
    [HttpPut("password")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Change password",
        Description = "Changes the password after verifying the current password. All active sessions remain valid."
    )]
    [SwaggerResponse(200, "Password changed successfully")]
    [SwaggerResponse(400, "Current password is incorrect")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _authService.ChangePasswordAsync(userId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Deactivate the current user account</summary>
    [HttpDelete("deactivate")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Deactivate account",
        Description = "Soft-deactivates the account. User cannot login until reactivated by Admin."
    )]
    [SwaggerResponse(200, "Account deactivated")]
    public async Task<IActionResult> DeactivateAccount()
    {
        var userId = GetCurrentUserId();
        var result = await _authService.DeactivateAccountAsync(userId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Login or register via Google or GitHub OAuth</summary>
    [HttpPost("oauth")]
    [SwaggerOperation(
        Summary = "OAuth login (Google / GitHub)",
        Description = "Login or auto-register using Google or GitHub OAuth. Provider must be 'google' or 'github'."
    )]
    [SwaggerResponse(200, "OAuth login successful — returns JWT token")]
    [SwaggerResponse(400, "Invalid OAuth provider or missing data")]
    public async Task<IActionResult> OAuthLogin([FromBody] OAuthLoginRequest request)
    {
        var result = await _authService.OAuthLoginAsync(
            request.Provider, request.ProviderId, request.Email, request.FullName);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Validate a JWT token (used by other microservices)</summary>
    [HttpGet("validate")]
    [SwaggerOperation(
        Summary = "Validate JWT token",
        Description = "Internal endpoint used by other microservices to verify a JWT token is valid and not expired."
    )]
    [SwaggerResponse(200, "Returns { valid: true/false }")]
    public IActionResult ValidateToken([FromQuery] string token)
    {
        var isValid = _authService.ValidateToken(token);
        return Ok(new { Valid = isValid });
    }

    // ---- Private Helper ----
    private int GetCurrentUserId()
    {
        var claim = User.FindFirst("userId")?.Value
                    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }
}

// ---- Extra request DTOs used only in this controller ----

/// <summary>Request body for token refresh</summary>
public class RefreshRequest
{
    /// <summary>The refresh token received during login</summary>
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>Request body for OAuth login</summary>
public class OAuthLoginRequest
{
    /// <summary>OAuth provider name: 'google' or 'github'</summary>
    public string Provider { get; set; } = string.Empty;
    /// <summary>Unique ID from the OAuth provider</summary>
    public string ProviderId { get; set; } = string.Empty;
    /// <summary>Email from OAuth provider</summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>Full name from OAuth provider</summary>
    public string FullName { get; set; } = string.Empty;
}
