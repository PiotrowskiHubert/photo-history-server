using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using photo_history_server.Domain.Entities;

namespace photo_history_server.Infrastructure.Persistence.Configurations;

public class PhotoTagConfiguration : IEntityTypeConfiguration<PhotoTag>
{
    public void Configure(EntityTypeBuilder<PhotoTag> builder)
    {
        builder.HasKey(pt => new { pt.PhotoId, pt.TagId });

        builder.HasOne(pt => pt.Photo)
            .WithMany(p => p.PhotoTags)
            .HasForeignKey(pt => pt.PhotoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pt => pt.Tag)
            .WithMany(t => t.PhotoTags)
            .HasForeignKey(pt => pt.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

