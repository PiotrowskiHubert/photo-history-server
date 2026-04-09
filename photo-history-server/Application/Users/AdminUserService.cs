using Microsoft.EntityFrameworkCore;
using photo_history_server.Application.Common.DTOs;
using photo_history_server.Infrastructure.Persistence;

namespace photo_history_server.Application.Users;

/// <summary>
/// Provides admin-level user management operations.
/// </summary>
public class AdminUserService
{
    private readonly AppDbContext _db;

    public AdminUserService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns all users sorted by CreatedAt descending.
    /// </summary>
    public async Task<List<AdminUserResponse>> GetAllUsersAsync()
    {
        return await _db.Users
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new AdminUserResponse(
                u.Id,
                u.Username,
                u.Email,
                u.Role.ToString(),
                u.CreatedAt,
                u.LastLoginAt,
                u.LastLogoutAt))
            .ToListAsync();
    }
}

