using Microsoft.EntityFrameworkCore;
using photo_history_server.Application.Common.DTOs;
using photo_history_server.Domain.Entities;
using photo_history_server.Domain.Enums;
using photo_history_server.Infrastructure.Persistence;

namespace photo_history_server.Application.Users;

/// <summary>
/// Handles registration of Admin users.
/// Protected by a hardcoded key — unlimited number of admins allowed.
/// </summary>
public class AdminRegistrationService
{
    private const string AdminRegistrationKey = "b7d3f1a2-9c4e-4b7d-8e1f-2a3c4d5e6f70";

    private readonly AppDbContext _db;
    private readonly AuthService _authService;

    public AdminRegistrationService(AppDbContext db, AuthService authService)
    {
        _db = db;
        _authService = authService;
    }

    public async Task<(AuthResponse? Response, AdminRegistrationResult Result)> RegisterAdminAsync(
        string providedKey, RegisterRequest request)
    {
        if (providedKey != AdminRegistrationKey)
            return (null, AdminRegistrationResult.InvalidKey);

        // No limit on number of admins — just check email uniqueness
        bool emailExists = await _db.Users.AnyAsync(u => u.Email == request.Email);
        if (emailExists)
            return (null, AdminRegistrationResult.EmailTaken);

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = UserRole.Admin
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var token = _authService.GenerateJwt(user);
        var response = new AuthResponse(token, user.Username, user.Email, user.Role.ToString(), user.AvatarUrl);

        return (response, AdminRegistrationResult.Success);
    }
}

/// <summary>
/// Possible outcomes of admin user registration attempt.
/// </summary>
public enum AdminRegistrationResult
{
    Success,
    InvalidKey,
    EmailTaken
}
