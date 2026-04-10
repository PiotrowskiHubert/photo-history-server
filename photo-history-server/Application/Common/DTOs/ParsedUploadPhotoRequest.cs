namespace photo_history_server.Application.Common.DTOs;

public record ParsedUploadPhotoRequest(
    IFormFile File,
    string? Description,
    DateTime? TakenAt,
    double Latitude,
    double Longitude,
    string? Address,
    string? Tags
);

