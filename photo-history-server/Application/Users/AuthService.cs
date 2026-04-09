using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using photo_history_server.Application.Common.DTOs;
using photo_history_server.Domain.Entities;
using photo_history_server.Domain.Enums;
using photo_history_server.Infrastructure.Persistence;
namespace photo_history_server.Application.Users;
public class AuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }
    /// <summary>
    /// Register a new user with email and password.
    /// Returns a RegisterResult with status indicating success or failure reason.
    /// </summary>
    public async Task<RegisterResult> RegisterAsync(RegisterRequest request)
    {
        // Check email uniqueness (case-insensitive)
        bool emailExists = await _db.Users.AnyAsync(u =>
            u.Email.ToLower() == request.Email.ToLower().Trim());
        if (emailExists) return new RegisterResult(null, RegisterStatus.EmailTaken);

        // Check username uniqueness (case-insensitive)
        bool usernameExists = await _db.Users.AnyAsync(u =>
            u.Username.ToLower() == request.Username.ToLower().Trim());
        if (usernameExists) return new RegisterResult(null, RegisterStatus.UsernameTaken);

        var user = new User
        {
            Username = request.Username.Trim(),
            Email = request.Email.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = UserRole.User
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var token = GenerateJwt(user);
        var response = new AuthResponse(token, user.Username, user.Email,
            user.Role.ToString(), user.AvatarUrl);
        return new RegisterResult(response, RegisterStatus.Success);
    }
    /// <summary>
    /// Authenticate user with email/username and password.
    /// Returns a LoginResult with status indicating success or failure reason.
    /// </summary>
    public async Task<LoginResult> LoginAsync(LoginRequest request)
    {
        // Allow login with either email or username (case-insensitive)
        var identifier = request.EmailOrUsername.Trim().ToLower();
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.Email.ToLower() == identifier ||
            u.Username.ToLower() == identifier);
        if (user is null || user.PasswordHash is null)
            return new LoginResult(null, LoginStatus.InvalidCredentials);

        // Check account status before verifying password
        if (user.IsBanned)
            return new LoginResult(null, LoginStatus.Banned);
        if (!user.IsActive)
            return new LoginResult(null, LoginStatus.Inactive);

        bool valid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!valid)
            return new LoginResult(null, LoginStatus.InvalidCredentials);

        // Record last login time
        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var token = GenerateJwt(user);
        var response = new AuthResponse(token, user.Username, user.Email, user.Role.ToString(), user.AvatarUrl);
        return new LoginResult(response, LoginStatus.Success);
    }

    /// <summary>
    /// Records the logout time for the given user.
    /// Returns false if user not found.
    /// </summary>
    public async Task<bool> LogoutAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return false;
        user.LastLogoutAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }
    /// <summary>
    /// Generate a JWT token for the given user. Public so other services can reuse it.
    /// </summary>
    public string GenerateJwt(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var expiryMinutes = int.Parse(_config["Jwt:ExpiryMinutes"] ?? "60");
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

/// <summary>
/// Result of a login attempt with a discriminated status.
/// </summary>
public record LoginResult(AuthResponse? Response, LoginStatus Status);

public enum LoginStatus
{
    Success,
    InvalidCredentials,
    Banned,
    Inactive
}

/// <summary>
/// Result of a registration attempt with a discriminated status.
/// </summary>
public record RegisterResult(AuthResponse? Response, RegisterStatus Status);

public enum RegisterStatus
{
    Success,
    EmailTaken,
    UsernameTaken
}

