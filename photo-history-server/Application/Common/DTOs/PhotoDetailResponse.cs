namespace photo_history_server.Application.Common.DTOs;

/// <summary>
/// Full photo detail response for the detail modal view.
/// </summary>
public record PhotoDetailResponse(
    Guid Id,
    string Url,
    string? Description,
    DateTime? TakenAt,
    string? Address,
    string UploaderUsername,
    Guid UserId);

