namespace photo_history_server.Domain.Entities;

public class Tag
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<PhotoTag> PhotoTags { get; set; } = new List<PhotoTag>();
}

