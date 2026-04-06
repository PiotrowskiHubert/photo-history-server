using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using photo_history_server.Domain.Entities;

namespace photo_history_server.Infrastructure.Persistence.Configurations;

public class PhotoConfiguration : IEntityTypeConfiguration<Photo>
{
    public void Configure(EntityTypeBuilder<Photo> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.FileName).HasMaxLength(256).IsRequired();
        builder.Property(p => p.FilePath).HasMaxLength(512).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(1000);

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

