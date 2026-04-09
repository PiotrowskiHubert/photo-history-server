namespace photo_history_server.Domain.Entities;

public class Photo
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public string ThumbnailName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? TakenAt { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Address { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Timestamp when an admin approved this photo. Null = awaiting review.</summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>ID of the admin who approved this photo. Null = not yet reviewed.</summary>
    public Guid? ReviewedBy { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}

