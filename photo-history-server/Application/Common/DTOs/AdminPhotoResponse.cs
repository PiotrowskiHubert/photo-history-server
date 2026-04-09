namespace photo_history_server.Application.Common.DTOs;

public record AdminPhotoResponse(
    Guid Id,
    string ThumbnailUrl,
    string? Description,
    DateTime? TakenAt,
    string? Address,
    DateTime UploadedAt,
    string UploaderUsername,
    Guid UserId);

