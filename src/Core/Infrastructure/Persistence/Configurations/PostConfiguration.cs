using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mootable.Domain.Entities;
using Mootable.Domain.Enums;

namespace Mootable.Infrastructure.Persistence.Configurations;

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.ToTable("Posts");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Content)
            .IsRequired()
            .HasMaxLength(5000); // Matrix'ten çıkış için derin düşüncelere izin ver

        builder.Property(p => p.HtmlContent)
            .HasMaxLength(10000);

        builder.Property(p => p.Category)
            .HasMaxLength(50);

        builder.Property(p => p.Tags)
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
            .HasMaxLength(500);

        builder.Property(p => p.MediaUrls)
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
            .HasMaxLength(2000);

        builder.Property(p => p.Visibility)
            .HasConversion<int>();

        // Self-referencing relationship for replies
        builder.HasOne(p => p.ParentPost)
            .WithMany(p => p.Replies)
            .HasForeignKey(p => p.ParentPostId)
            .OnDelete(DeleteBehavior.Cascade);

        // User relationship
        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(p => p.UserId);
        builder.HasIndex(p => p.Category);
        builder.HasIndex(p => p.CreatedAt);
        builder.HasIndex(p => p.ParentPostId);
        builder.HasIndex(p => new { p.Visibility, p.CreatedAt });

        // For Matrix enlightenment scoring
        builder.HasIndex(p => p.EnlightenmentScore);
    }
}