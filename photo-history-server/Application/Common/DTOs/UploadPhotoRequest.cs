namespace photo_history_server.Application.Common.DTOs;

/// <summary>
/// Request model for photo upload. Plain class for multipart/form-data binding.
/// All nullable/numeric types are strings to avoid culture-sensitive parsing issues.
/// </summary>
public class UploadPhotoRequest
{
    public IFormFile File { get; set; } = null!;
    public string? Description { get; set; }
    public string? TakenAt { get; set; }      // ISO 8601 string, parsed manually
    public string Latitude { get; set; } = "0";
    public string Longitude { get; set; } = "0";
    public string? Address { get; set; }
}

