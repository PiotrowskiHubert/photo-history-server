using Microsoft.EntityFrameworkCore;
using photo_history_server.Application.Common.DTOs;
using photo_history_server.Domain.Enums;
using photo_history_server.Infrastructure.Persistence;

namespace photo_history_server.Application.Users;

/// <summary>
/// Provides admin-level user management operations.
/// </summary>
public class AdminUserService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AdminUserService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
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
                u.LastLogoutAt,
                u.IsActive,
                u.IsBanned))
            .ToListAsync();
    }

    /// <summary>
    /// Returns unreviewed photos from all users sorted by UploadedAt descending.
    /// </summary>
    public async Task<List<AdminPhotoResponse>> GetAllPhotosAsync()
    {
        return await _db.Photos
            .Include(p => p.User)
            .Where(p => p.ReviewedAt == null)
            .OrderByDescending(p => p.UploadedAt)
            .Select(p => new AdminPhotoResponse(
                p.Id,
                "/uploads/" + p.FileName,       // Url — full image
                "/uploads/" + p.ThumbnailName,  // ThumbnailUrl — thumbnail
                p.Description,
                p.TakenAt,
                p.Address,
                p.UploadedAt,
                p.User.Username,
                p.UserId,
                p.ReviewedAt,
                p.ReviewedBy))
            .ToListAsync();
    }

    /// <summary>
    /// Returns the count of photos awaiting review.
    /// </summary>
    public async Task<int> GetUnreviewedCountAsync()
    {
        return await _db.Photos.CountAsync(p => p.ReviewedAt == null);
    }

    /// <summary>
    /// Marks a photo as reviewed by the given admin.
    /// Returns false if photo not found.
    /// </summary>
    public async Task<bool> ReviewPhotoAsync(Guid photoId, Guid adminId)
    {
        var photo = await _db.Photos.FindAsync(photoId);
        if (photo is null) return false;

        photo.ReviewedAt = DateTime.UtcNow;
        photo.ReviewedBy = adminId;
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Clears the review status of a photo (moves it back to unreviewed queue).
    /// Returns false if photo not found.
    /// </summary>
    public async Task<bool> UnreviewPhotoAsync(Guid photoId)
    {
        var photo = await _db.Photos.FindAsync(photoId);
        if (photo is null) return false;

        photo.ReviewedAt = null;
        photo.ReviewedBy = null;
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Bans a user. Cannot ban Admin or System users.
    /// </summary>
    public async Task<AdminActionResult> BanUserAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return AdminActionResult.NotFound;
        if (user.Role >= UserRole.Admin) return AdminActionResult.Forbidden;

        user.IsBanned = true;
        await _db.SaveChangesAsync();
        return AdminActionResult.Success;
    }

    /// <summary>
    /// Deactivates a user. Cannot deactivate Admin or System users.
    /// </summary>
    public async Task<AdminActionResult> DeactivateUserAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return AdminActionResult.NotFound;
        if (user.Role >= UserRole.Admin) return AdminActionResult.Forbidden;

        user.IsActive = false;
        await _db.SaveChangesAsync();
        return AdminActionResult.Success;
    }

    /// <summary>
    /// Deletes a photo and its files from disk (reject).
    /// Returns false if photo not found.
    /// </summary>
    public async Task<bool> RejectPhotoAsync(Guid photoId)
    {
        var photo = await _db.Photos.FindAsync(photoId);
        if (photo is null) return false;

        var storagePath = _config["Storage:PhotosPath"]!;
        var filePath = Path.Combine(storagePath, photo.FileName);
        var thumbPath = Path.Combine(storagePath, photo.ThumbnailName);
        if (File.Exists(filePath)) File.Delete(filePath);
        if (File.Exists(thumbPath)) File.Delete(thumbPath);

        _db.Photos.Remove(photo);
        await _db.SaveChangesAsync();
        return true;
    }
}

/// <summary>
/// Possible outcomes of an admin action on a user.
/// </summary>
public enum AdminActionResult
{
    Success,
    NotFound,
    Forbidden
}
