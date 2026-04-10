// TODO: run migration: dotnet ef migrations add AddPhotoThumbnailName && dotnet ef database update
using System.Globalization;
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

        // Handle tags
        if (!string.IsNullOrWhiteSpace(request.Tags))
        {
            var tagNames = request.Tags.Split(',')
                .Select(t => t.Trim())
                .Where(t => t.Length > 0)
                .ToList();

            foreach (var tagName in tagNames)
            {
                var tag = await _db.Tags
                    .FirstOrDefaultAsync(t => t.Name.ToLower() == tagName.ToLower());

                if (tag is null)
                {
                    tag = new Tag { Name = tagName };
                    _db.Tags.Add(tag);
                }

                _db.PhotoTags.Add(new PhotoTag { Photo = photo, Tag = tag });
            }
        }

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
        int? fromYear, int? toYear,
        IEnumerable<string>? tags = null)
    {
        var query = _db.Photos
            .Where(p =>
                p.Latitude >= minLat && p.Latitude <= maxLat &&
                p.Longitude >= minLng && p.Longitude <= maxLng);

        if (fromYear.HasValue)
            query = query.Where(p => p.TakenAt == null || p.TakenAt.Value.Year >= fromYear);

        if (toYear.HasValue)
            query = query.Where(p => p.TakenAt == null || p.TakenAt.Value.Year <= toYear);

        var tagList = tags?.ToList();
        if (tagList is { Count: > 0 })
        {
            var lowerTags = tagList.Select(t => t.ToLower()).ToList();
            query = query.Where(p =>
                p.PhotoTags.Any(pt => lowerTags.Contains(pt.Tag.Name.ToLower())));
        }

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
            .Include(p => p.PhotoTags).ThenInclude(pt => pt.Tag)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (photo is null) return null;

        var tagNames = photo.PhotoTags.Select(pt => pt.Tag.Name).ToList();

        return new PhotoDetailResponse(
            photo.Id,
            "/uploads/" + photo.FileName,
            photo.Description,
            photo.TakenAt,
            photo.Address,
            photo.User.Username,
            photo.UserId,
            tagNames);
    }

    /// <summary>
    /// Return all photos uploaded by a specific user, sorted by upload date descending.
    /// </summary>
    public async Task<List<UserPhotoResponse>> GetByUserAsync(Guid userId)
    {
        return await _db.Photos
            .Where(p => p.UserId == userId)
            .Include(p => p.PhotoTags).ThenInclude(pt => pt.Tag)
            .OrderByDescending(p => p.UploadedAt)
            .Select(p => new UserPhotoResponse(
                p.Id,
                "/uploads/" + p.ThumbnailName,
                p.Description,
                p.TakenAt,
                p.Address,
                p.UploadedAt,
                p.PhotoTags.Select(pt => pt.Tag.Name).ToList(),
                p.ReviewedAt))
            .ToListAsync();
    }

    /// <summary>
    /// Update photo metadata (description and/or takenAt). Only the owner can update.
    /// </summary>
    public async Task<bool> UpdateAsync(Guid id, Guid userId, UpdatePhotoRequest request)
    {
        var photo = await _db.Photos
            .Include(p => p.PhotoTags)
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
        if (photo is null) return false;

        if (request.Description is not null)
            photo.Description = request.Description;

        if (request.TakenAt is not null)
        {
            if (DateTime.TryParse(request.TakenAt, null, DateTimeStyles.RoundtripKind, out var dt))
                photo.TakenAt = dt;
            else
                photo.TakenAt = null;
        }

        // Handle tags — null means "don't touch", non-null (even empty) means "replace"
        if (request.Tags is not null)
        {
            _db.PhotoTags.RemoveRange(photo.PhotoTags);

            if (!string.IsNullOrWhiteSpace(request.Tags))
            {
                var tagNames = request.Tags.Split(',')
                    .Select(t => t.Trim())
                    .Where(t => t.Length > 0)
                    .ToList();

                foreach (var tagName in tagNames)
                {
                    var tag = await _db.Tags
                        .FirstOrDefaultAsync(t => t.Name.ToLower() == tagName.ToLower());

                    if (tag is null)
                    {
                        tag = new Tag { Name = tagName };
                        _db.Tags.Add(tag);
                    }

                    _db.PhotoTags.Add(new PhotoTag { PhotoId = photo.Id, Tag = tag });
                }
            }
        }

        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Replace the photo file and regenerate thumbnail. Only the owner can replace.
    /// </summary>
    public async Task<bool> ReplaceImageAsync(Guid id, Guid userId, IFormFile file)
    {
        var photo = await _db.Photos.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
        if (photo is null) return false;

        var storagePath = _config["Storage:PhotosPath"]!;

        // Delete old files if they exist
        var oldPath = Path.Combine(storagePath, photo.FileName);
        var oldThumb = Path.Combine(storagePath, photo.ThumbnailName);
        if (File.Exists(oldPath)) File.Delete(oldPath);
        if (File.Exists(oldThumb)) File.Delete(oldThumb);

        // Save new file
        var extension = Path.GetExtension(file.FileName);
        var newFileName = Guid.NewGuid() + extension;
        var newPath = Path.Combine(storagePath, newFileName);
        await using (var stream = new FileStream(newPath, FileMode.Create))
            await file.CopyToAsync(stream);

        // Regenerate thumbnail
        var newThumbName = Path.GetFileNameWithoutExtension(newFileName) + "_thumb.jpg";
        var newThumbPath = Path.Combine(storagePath, newThumbName);
        using (var image = await Image.LoadAsync(newPath))
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(300, 300),
                Mode = ResizeMode.Max
            }));
            await image.SaveAsJpegAsync(newThumbPath, new JpegEncoder { Quality = 80 });
        }

        photo.FileName = newFileName;
        photo.FilePath = newPath;
        photo.ThumbnailName = newThumbName;
        photo.OriginalName = file.FileName;
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Delete a photo and its thumbnail from disk and DB. Only the owner can delete.
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id, Guid userId)
    {
        var photo = await _db.Photos.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
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

