using Microsoft.AspNetCore.Http;

namespace photo_history_server.Application.Common.DTOs;

/// <summary>
/// Request model for photo upload. Plain class for multipart/form-data binding.
/// </summary>
public class UploadPhotoRequest
{
    public IFormFile File { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime? TakenAt { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Address { get; set; }
}

