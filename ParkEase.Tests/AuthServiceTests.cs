using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using ParkEase.AuthService.Data;
using ParkEase.AuthService.DTOs;
using ParkEase.AuthService.Entities;
using ParkEase.AuthService.Helpers;
using ParkEase.AuthService.Interfaces;

namespace ParkEase.Tests.AuthServiceTests;

[TestFixture]
public class AuthServiceTests
{
    private Mock<IUserRepository> _userRepoMock = null!;
    private AuthDbContext _dbContext = null!;
    private ParkEase.AuthService.Services.AuthService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepoMock = new Mock<IUserRepository>();

        // Use in-memory EF Core database
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new AuthDbContext(options);

        // Use real JwtHelper with test config — JwtHelper methods are non-virtual, can't be mocked
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"]   = "ParkEase@SuperSecretKey#2026!ChangeThis",
                ["Jwt:Issuer"]   = "ParkEase.AuthService",
                ["Jwt:Audience"] = "ParkEase.Clients"
            })
            .Build();

        var jwtHelper = new JwtHelper(config);
        _service = new ParkEase.AuthService.Services.AuthService(_userRepoMock.Object, _dbContext, jwtHelper);
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
    }

    // ════════════════════════════════════════════════
    // REGISTER
    // ════════════════════════════════════════════════

    [Test]
    public async Task Register_ValidDriverRequest_ReturnsSuccess()
    {
        var request = ValidRegisterRequest("DRIVER");
        _userRepoMock.Setup(r => r.ExistsByEmailAsync(request.Email)).ReturnsAsync(false);
        _userRepoMock.Setup(r => r.CreateAsync(It.IsAny<User>()))
                     .ReturnsAsync((User u) => { u.UserId = 1; return u; });

        var result = await _service.RegisterAsync(request);

        result.Success.Should().BeTrue();
        result.Data!.Token.Should().NotBeNullOrEmpty();
        result.Data.User.Role.Should().Be("DRIVER");
    }

    [Test]
    public async Task Register_ValidManagerRequest_ReturnsSuccess()
    {
        var request = ValidRegisterRequest("MANAGER");
        _userRepoMock.Setup(r => r.ExistsByEmailAsync(request.Email)).ReturnsAsync(false);
        _userRepoMock.Setup(r => r.CreateAsync(It.IsAny<User>()))
                     .ReturnsAsync((User u) => { u.UserId = 2; return u; });

        var result = await _service.RegisterAsync(request);

        result.Success.Should().BeTrue();
        result.Data!.User.Role.Should().Be("MANAGER");
    }

    [Test]
    public async Task Register_DuplicateEmail_ReturnsFail()
    {
        var request = ValidRegisterRequest("DRIVER");
        _userRepoMock.Setup(r => r.ExistsByEmailAsync(request.Email)).ReturnsAsync(true);

        var result = await _service.RegisterAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Email already registered");
    }

    [Test]
    public async Task Register_InvalidRole_ReturnsFail()
    {
        var request = ValidRegisterRequest("ADMIN"); // ADMIN cannot self-register

        var result = await _service.RegisterAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Invalid role");
        result.Message.Should().Contain("DRIVER or MANAGER");
    }

    [Test]
    public async Task Register_RoleIsCaseInsensitive()
    {
        var request = ValidRegisterRequest("driver"); // lowercase
        _userRepoMock.Setup(r => r.ExistsByEmailAsync(request.Email)).ReturnsAsync(false);
        _userRepoMock.Setup(r => r.CreateAsync(It.IsAny<User>()))
                     .ReturnsAsync((User u) => u);

        var result = await _service.RegisterAsync(request);

        result.Success.Should().BeTrue();
        result.Data!.User.Role.Should().Be("DRIVER");
    }

    [Test]
    public async Task Register_EmailIsStoredLowercase()
    {
        var request = ValidRegisterRequest("DRIVER");
        request.Email = "Test@Example.COM";
        _userRepoMock.Setup(r => r.ExistsByEmailAsync(request.Email)).ReturnsAsync(false);

        User? captured = null;
        _userRepoMock.Setup(r => r.CreateAsync(It.IsAny<User>()))
                     .Callback<User>(u => captured = u)
                     .ReturnsAsync((User u) => u);

        await _service.RegisterAsync(request);

        captured!.Email.Should().Be("test@example.com");
    }

    [Test]
    public async Task Register_PasswordIsHashed()
    {
        var request = ValidRegisterRequest("DRIVER");
        request.Password = "PlainTextPassword123";
        _userRepoMock.Setup(r => r.ExistsByEmailAsync(request.Email)).ReturnsAsync(false);

        User? captured = null;
        _userRepoMock.Setup(r => r.CreateAsync(It.IsAny<User>()))
                     .Callback<User>(u => captured = u)
                     .ReturnsAsync((User u) => u);

        await _service.RegisterAsync(request);

        captured!.PasswordHash.Should().NotBe("PlainTextPassword123");
        BCrypt.Net.BCrypt.Verify("PlainTextPassword123", captured.PasswordHash).Should().BeTrue();
    }

    // ════════════════════════════════════════════════
    // LOGIN
    // ════════════════════════════════════════════════

    [Test]
    public async Task Login_ValidCredentials_ReturnsSuccess()
    {
        var user = ActiveUser();
        _userRepoMock.Setup(r => r.FindByEmailAsync(user.Email)).ReturnsAsync(user);

        var result = await _service.LoginAsync(new LoginRequest
        {
            Email    = user.Email,
            Password = "password123"
        });

        result.Success.Should().BeTrue();
        result.Data!.Token.Should().NotBeNullOrEmpty();
        result.Data.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task Login_WrongPassword_ReturnsFail()
    {
        var user = ActiveUser();
        _userRepoMock.Setup(r => r.FindByEmailAsync(user.Email)).ReturnsAsync(user);

        var result = await _service.LoginAsync(new LoginRequest
        {
            Email    = user.Email,
            Password = "wrongpassword"
        });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Invalid email or password");
    }

    [Test]
    public async Task Login_UserNotFound_ReturnsFail()
    {
        _userRepoMock.Setup(r => r.FindByEmailAsync("notfound@test.com"))
                     .ReturnsAsync((User?)null);

        var result = await _service.LoginAsync(new LoginRequest
        {
            Email    = "notfound@test.com",
            Password = "password123"
        });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Invalid email or password");
    }

    [Test]
    public async Task Login_DeactivatedAccount_ReturnsFail()
    {
        var user = ActiveUser();
        user.IsActive = false;
        _userRepoMock.Setup(r => r.FindByEmailAsync(user.Email)).ReturnsAsync(user);

        var result = await _service.LoginAsync(new LoginRequest
        {
            Email    = user.Email,
            Password = "password123"
        });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("deactivated");
    }

    // ════════════════════════════════════════════════
    // PROFILE
    // ════════════════════════════════════════════════

    [Test]
    public async Task GetProfile_ExistingUser_ReturnsUserDto()
    {
        var user = ActiveUser();
        _userRepoMock.Setup(r => r.FindByUserIdAsync(1)).ReturnsAsync(user);

        var result = await _service.GetProfileAsync(1);

        result.Success.Should().BeTrue();
        result.Data!.Email.Should().Be(user.Email);
        result.Data.FullName.Should().Be(user.FullName);
    }

    [Test]
    public async Task GetProfile_NotFound_ReturnsFail()
    {
        _userRepoMock.Setup(r => r.FindByUserIdAsync(99)).ReturnsAsync((User?)null);

        var result = await _service.GetProfileAsync(99);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("User not found");
    }

    [Test]
    public async Task UpdateProfile_PartialUpdate_OnlyChangesProvidedFields()
    {
        var user = ActiveUser();
        _userRepoMock.Setup(r => r.FindByUserIdAsync(1)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        var result = await _service.UpdateProfileAsync(1, new UpdateProfileRequest
        {
            FullName = "Updated Name",
            Phone    = null // not changing phone
        });

        result.Success.Should().BeTrue();
        result.Data!.FullName.Should().Be("Updated Name");
        result.Data.Phone.Should().Be(user.Phone); // unchanged
    }

    [Test]
    public async Task UpdateProfile_NotFound_ReturnsFail()
    {
        _userRepoMock.Setup(r => r.FindByUserIdAsync(99)).ReturnsAsync((User?)null);

        var result = await _service.UpdateProfileAsync(99, new UpdateProfileRequest { FullName = "Test" });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("User not found");
    }

    // ════════════════════════════════════════════════
    // CHANGE PASSWORD
    // ════════════════════════════════════════════════

    [Test]
    public async Task ChangePassword_CorrectCurrentPassword_ReturnsSuccess()
    {
        var user = ActiveUser();
        _userRepoMock.Setup(r => r.FindByUserIdAsync(1)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        var result = await _service.ChangePasswordAsync(1, new ChangePasswordRequest
        {
            CurrentPassword = "password123",
            NewPassword     = "newpassword456"
        });

        result.Success.Should().BeTrue();
        BCrypt.Net.BCrypt.Verify("newpassword456", user.PasswordHash).Should().BeTrue();
    }

    [Test]
    public async Task ChangePassword_WrongCurrentPassword_ReturnsFail()
    {
        var user = ActiveUser();
        _userRepoMock.Setup(r => r.FindByUserIdAsync(1)).ReturnsAsync(user);

        var result = await _service.ChangePasswordAsync(1, new ChangePasswordRequest
        {
            CurrentPassword = "wrongpassword",
            NewPassword     = "newpassword456"
        });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Current password is incorrect");
    }

    [Test]
    public async Task ChangePassword_UserNotFound_ReturnsFail()
    {
        _userRepoMock.Setup(r => r.FindByUserIdAsync(99)).ReturnsAsync((User?)null);

        var result = await _service.ChangePasswordAsync(99, new ChangePasswordRequest
        {
            CurrentPassword = "password123",
            NewPassword     = "new"
        });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("User not found");
    }

    // ════════════════════════════════════════════════
    // DEACTIVATE
    // ════════════════════════════════════════════════

    [Test]
    public async Task DeactivateAccount_ExistingUser_SetsInactive()
    {
        var user = ActiveUser();
        _userRepoMock.Setup(r => r.FindByUserIdAsync(1)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        var result = await _service.DeactivateAccountAsync(1);

        result.Success.Should().BeTrue();
        user.IsActive.Should().BeFalse();
    }

    [Test]
    public async Task DeactivateAccount_NotFound_ReturnsFail()
    {
        _userRepoMock.Setup(r => r.FindByUserIdAsync(99)).ReturnsAsync((User?)null);

        var result = await _service.DeactivateAccountAsync(99);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("User not found");
    }

    // ════════════════════════════════════════════════
    // OAUTH
    // ════════════════════════════════════════════════

    [Test]
    public async Task OAuthLogin_NewUser_RegistersAsDriver()
    {
        _userRepoMock.Setup(r => r.FindByOAuthAsync("google", "oauth123")).ReturnsAsync((User?)null);
        _userRepoMock.Setup(r => r.FindByEmailAsync("oauth@test.com")).ReturnsAsync((User?)null);

        User? captured = null;
        _userRepoMock.Setup(r => r.CreateAsync(It.IsAny<User>()))
                     .Callback<User>(u => captured = u)
                     .ReturnsAsync((User u) => { u.UserId = 10; return u; });

        var result = await _service.OAuthLoginAsync("google", "oauth123", "oauth@test.com", "OAuth User");

        result.Success.Should().BeTrue();
        captured!.Role.Should().Be("DRIVER");
        captured.OAuthProvider.Should().Be("google");
        captured.OAuthProviderId.Should().Be("oauth123");
    }

    [Test]
    public async Task OAuthLogin_ExistingOAuthUser_LogsIn()
    {
        var user = ActiveUser();
        user.OAuthProvider   = "google";
        user.OAuthProviderId = "oauth123";
        _userRepoMock.Setup(r => r.FindByOAuthAsync("google", "oauth123")).ReturnsAsync(user);

        var result = await _service.OAuthLoginAsync("google", "oauth123", user.Email, user.FullName);

        result.Success.Should().BeTrue();
        result.Data!.Token.Should().NotBeNullOrEmpty();
        _userRepoMock.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Never);
    }

    [Test]
    public async Task OAuthLogin_ExistingEmailUser_LinksAccount()
    {
        var user = ActiveUser();
        _userRepoMock.Setup(r => r.FindByOAuthAsync("github", "gh123")).ReturnsAsync((User?)null);
        _userRepoMock.Setup(r => r.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        var result = await _service.OAuthLoginAsync("github", "gh123", user.Email, user.FullName);

        result.Success.Should().BeTrue();
        user.OAuthProvider.Should().Be("github");
        user.OAuthProviderId.Should().Be("gh123");
        _userRepoMock.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Never);
    }

    // ════════════════════════════════════════════════
    // REFRESH TOKEN
    // ════════════════════════════════════════════════

    [Test]
    public async Task RefreshToken_ValidToken_ReturnsNewTokens()
    {
        var user = ActiveUser();

        var storedToken = new RefreshToken
        {
            UserId    = user.UserId,
            Token     = "valid-refresh-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.RefreshTokens.Add(storedToken);
        await _dbContext.SaveChangesAsync();

        _userRepoMock.Setup(r => r.FindByUserIdAsync(user.UserId)).ReturnsAsync(user);

        var result = await _service.RefreshTokenAsync("valid-refresh-token");

        result.Success.Should().BeTrue();
        result.Data!.Token.Should().NotBeNullOrEmpty();
        storedToken.IsRevoked.Should().BeTrue();
    }

    [Test]
    public async Task RefreshToken_ExpiredToken_ReturnsFail()
    {
        var expiredToken = new RefreshToken
        {
            UserId    = 1,
            Token     = "expired-token",
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow.AddDays(-8)
        };
        _dbContext.RefreshTokens.Add(expiredToken);
        await _dbContext.SaveChangesAsync();

        var result = await _service.RefreshTokenAsync("expired-token");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Invalid or expired refresh token");
    }

    [Test]
    public async Task RefreshToken_RevokedToken_ReturnsFail()
    {
        var revokedToken = new RefreshToken
        {
            UserId    = 1,
            Token     = "revoked-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = true,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.RefreshTokens.Add(revokedToken);
        await _dbContext.SaveChangesAsync();

        var result = await _service.RefreshTokenAsync("revoked-token");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Invalid or expired refresh token");
    }

    [Test]
    public async Task RefreshToken_NonExistentToken_ReturnsFail()
    {
        var result = await _service.RefreshTokenAsync("does-not-exist");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Invalid or expired refresh token");
    }

    // ════════════════════════════════════════════════
    // HELPERS
    // ════════════════════════════════════════════════

    private static RegisterRequest ValidRegisterRequest(string role) => new()
    {
        FullName = "Test User",
        Email    = "test@parkease.com",
        Password = "password123",
        Phone    = "9876543210",
        Role     = role
    };

    private static User ActiveUser() => new()
    {
        UserId       = 1,
        FullName     = "Test User",
        Email        = "test@parkease.com",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
        Phone        = "9876543210",
        Role         = "DRIVER",
        IsActive     = true,
        CreatedAt    = DateTime.UtcNow
    };
}