namespace photo_history_server.Application.Common.DTOs;

public record AdminPhotoResponse(
    Guid Id,
    string Url,           // full image — "/uploads/" + FileName
    string ThumbnailUrl,  // thumbnail — "/uploads/" + ThumbnailName
    string? Description,
    DateTime? TakenAt,
    string? Address,
    DateTime UploadedAt,
    string UploaderUsername,
    Guid UserId,
    DateTime? ReviewedAt,
    Guid? ReviewedBy,
    IReadOnlyList<string> Tags);

