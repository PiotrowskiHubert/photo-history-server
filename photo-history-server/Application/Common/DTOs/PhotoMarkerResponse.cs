namespace photo_history_server.Application.Common.DTOs;

/// <summary>
/// Lightweight response for map marker rendering — includes thumbnail URL for map pins.
/// </summary>
public record PhotoMarkerResponse(
    Guid Id,
    double Latitude,
    double Longitude,
    DateTime? TakenAt,
    string ThumbnailUrl);

