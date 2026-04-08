// TODO: run migration: dotnet ef migrations add AddPhotoThumbnailName && dotnet ef database update
using Microsoft.EntityFrameworkCore;
using photo_history_server.Application.Common.DTOs;
using photo_history_server.Domain.Entities;
using photo_history_server.Infrastructure.Persistence;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

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

        // Generate 300x300 JPEG thumbnail (preserve aspect ratio)
        var thumbnailName = Path.GetFileNameWithoutExtension(fileName) + "_thumb.jpg";
        var thumbnailPath = Path.Combine(storagePath, thumbnailName);
        using (var image = await Image.LoadAsync(fullPath))
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(300, 300),
                Mode = ResizeMode.Max
            }));
            await image.SaveAsJpegAsync(thumbnailPath, new JpegEncoder { Quality = 80 });
        }

        var photo = new Photo
        {
            FileName = fileName,
            FilePath = fullPath,
            ThumbnailName = thumbnailName,
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
    /// Return lightweight marker data for photos within the given bounding box,
    /// optionally filtered by year range. Photos with null TakenAt are always included.
    /// </summary>
    public async Task<List<PhotoMarkerResponse>> GetInBoundsAsync(
        double minLat, double maxLat, double minLng, double maxLng,
        int? fromYear, int? toYear)
    {
        var query = _db.Photos
            .Where(p =>
                p.Latitude >= minLat && p.Latitude <= maxLat &&
                p.Longitude >= minLng && p.Longitude <= maxLng);

        if (fromYear.HasValue)
            query = query.Where(p => p.TakenAt == null || p.TakenAt.Value.Year >= fromYear);

        if (toYear.HasValue)
            query = query.Where(p => p.TakenAt == null || p.TakenAt.Value.Year <= toYear);

        return await query
            .OrderByDescending(p => p.UploadedAt)
            .Select(p => new PhotoMarkerResponse(
                p.Id,
                p.Latitude,
                p.Longitude,
                p.TakenAt,
                "/uploads/" + p.ThumbnailName))
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

