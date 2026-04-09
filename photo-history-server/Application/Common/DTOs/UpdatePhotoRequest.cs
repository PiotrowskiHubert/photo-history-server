namespace photo_history_server.Application.Common.DTOs;

/// <summary>
/// Request model for updating photo metadata.
/// </summary>
public record UpdatePhotoRequest(
    string? Description,
    string? TakenAt);   // ISO 8601 string, nullable

