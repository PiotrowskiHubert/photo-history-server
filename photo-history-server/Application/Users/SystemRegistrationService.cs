using Microsoft.EntityFrameworkCore;
using photo_history_server.Application.Common.DTOs;
using photo_history_server.Domain.Entities;
using photo_history_server.Domain.Enums;
using photo_history_server.Infrastructure.Persistence;

namespace photo_history_server.Application.Users;

/// <summary>
/// Handles one-time registration of the single SYSTEM user,
/// protected by a hardcoded GUID secret key.
/// </summary>
public class SystemRegistrationService
{
    private const string SystemRegistrationKey = "a3f1e2d4-7b6c-4e8a-9d0f-1c2b3a4e5f60";

    private readonly AppDbContext _db;
    private readonly AuthService _authService;

    public SystemRegistrationService(AppDbContext db, AuthService authService)
    {
        _db = db;
        _authService = authService;
    }

    /// <summary>
    /// Register the single SYSTEM user.
    /// Returns null if the key is invalid or a SYSTEM user already exists.
    /// The caller should distinguish between the two null cases via the out parameter.
    /// </summary>
    public async Task<(AuthResponse? Response, SystemRegistrationResult Result)> RegisterSystemAsync(
        string providedKey, RegisterRequest request)
    {
        // Validate the secret registration key
        if (providedKey != SystemRegistrationKey)
            return (null, SystemRegistrationResult.InvalidKey);

        // Ensure only one SYSTEM user can ever exist
        bool systemExists = await _db.Users.AnyAsync(u => u.Role == UserRole.System);
        if (systemExists)
            return (null, SystemRegistrationResult.AlreadyExists);

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = UserRole.System
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var token = _authService.GenerateJwt(user);
        var response = new AuthResponse(token, user.Username, user.Email, user.Role.ToString(), user.AvatarUrl);

        return (response, SystemRegistrationResult.Success);
    }
}

/// <summary>
/// Possible outcomes of system user registration attempt.
/// </summary>
public enum SystemRegistrationResult
{
    Success,
    InvalidKey,
    AlreadyExists
}

