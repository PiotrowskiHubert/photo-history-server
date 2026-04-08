using Microsoft.EntityFrameworkCore;
using photo_history_server.Application.Common.DTOs;
using photo_history_server.Domain.Entities;
using photo_history_server.Infrastructure.Persistence;

namespace photo_history_server.Application.Photos;

/// <summary>
/// Handles photo upload and retrieval operations.
/// </summary>
public class PhotoService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public PhotoService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// Save uploaded file to disk and create a Photo record in the database.
    /// </summary>
    public async Task<PhotoResponse> UploadAsync(ParsedUploadPhotoRequest request, Guid userId)
    {
        var storagePath = _config["Storage:PhotosPath"]!;
        Directory.CreateDirectory(storagePath);

        // Generate a unique file name preserving the original extension
        var extension = Path.GetExtension(request.File.FileName);
        var fileName = Guid.NewGuid() + extension;
        var fullPath = Path.Combine(storagePath, fileName);

        // Save file to disk
        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await request.File.CopyToAsync(stream);
        }

        var photo = new Photo
        {
            FileName = fileName,
            FilePath = fullPath,
            OriginalName = request.File.FileName,
            Description = request.Description,
            TakenAt = request.TakenAt,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Address = request.Address,
            UploadedAt = DateTime.UtcNow,
            UserId = userId
        };

        _db.Photos.Add(photo);
        await _db.SaveChangesAsync();

        return new PhotoResponse(
            photo.Id,
            photo.FileName,
            "/uploads/" + photo.FileName,
            photo.Description,
            photo.TakenAt,
            photo.Latitude,
            photo.Longitude,
            photo.Address,
            photo.UploadedAt,
            photo.UserId);
    }

    /// <summary>
    /// Return lightweight marker data for photos within the given bounding box.
    /// </summary>
    public async Task<List<PhotoMarkerResponse>> GetInBoundsAsync(
        double minLat, double maxLat, double minLng, double maxLng)
    {
        return await _db.Photos
            .Where(p =>
                p.Latitude >= minLat && p.Latitude <= maxLat &&
                p.Longitude >= minLng && p.Longitude <= maxLng)
            .OrderByDescending(p => p.UploadedAt)
            .Select(p => new PhotoMarkerResponse(
                p.Id,
                p.Latitude,
                p.Longitude,
                p.TakenAt))
            .ToListAsync();
    }

    /// <summary>
    /// Return full photo details including uploader username.
    /// Returns null if photo not found.
    /// </summary>
    public async Task<PhotoDetailResponse?> GetByIdAsync(Guid id)
    {
        var photo = await _db.Photos
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (photo is null) return null;

        return new PhotoDetailResponse(
            photo.Id,
            "/uploads/" + photo.FileName,
            photo.Description,
            photo.TakenAt,
            photo.Address,
            photo.User.Username);
    }
}

