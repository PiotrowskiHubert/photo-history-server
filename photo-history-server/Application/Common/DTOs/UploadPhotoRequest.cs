namespace photo_history_server.Application.Common.DTOs;

/// <summary>
/// Request model for photo upload. Plain class for multipart/form-data binding.
/// Latitude/Longitude are strings to avoid culture-sensitive decimal separator issues.
/// </summary>
public class UploadPhotoRequest
{
    public IFormFile File { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime? TakenAt { get; set; }
    public string Latitude { get; set; } = "0";
    public string Longitude { get; set; } = "0";
    public string? Address { get; set; }
}

