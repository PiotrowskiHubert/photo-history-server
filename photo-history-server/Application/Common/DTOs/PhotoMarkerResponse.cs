namespace photo_history_server.Application.Common.DTOs;

/// <summary>
/// Lightweight response for map marker rendering — no image URL, no address.
/// </summary>
public record PhotoMarkerResponse(
    Guid Id,
    double Latitude,
    double Longitude,
    DateTime? TakenAt);

