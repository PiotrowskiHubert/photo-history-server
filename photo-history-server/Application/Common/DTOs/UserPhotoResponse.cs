namespace photo_history_server.Application.Common.DTOs;

/// <summary>
/// Response for photos belonging to the authenticated user.
/// </summary>
public record UserPhotoResponse(
    Guid Id,
    string ThumbnailUrl,
    string? Description,
    DateTime? TakenAt,
    string? Address,
    DateTime UploadedAt);

