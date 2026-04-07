namespace photo_history_server.Application.Common.DTOs;

public record PhotoResponse(
    Guid Id,
    string FileName,
    string Url,
    string? Description,
    DateTime? TakenAt,
    double Latitude,
    double Longitude,
    string? Address,
    DateTime UploadedAt,
    Guid UserId);

