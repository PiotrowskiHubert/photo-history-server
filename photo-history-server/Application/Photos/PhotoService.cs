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
    public async Task<PhotoResponse> UploadAsync(UploadPhotoRequest request, Guid userId)
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

        return ToResponse(photo);
    }

    /// <summary>
    /// Return all photos from the database.
    /// </summary>
    public async Task<List<PhotoResponse>> GetAllAsync()
    {
        var photos = await _db.Photos
            .OrderByDescending(p => p.UploadedAt)
            .ToListAsync();

        return photos.Select(ToResponse).ToList();
    }

    private static PhotoResponse ToResponse(Photo photo) =>
        new(
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

