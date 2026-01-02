using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mootable.Domain.Entities;

namespace Mootable.Infrastructure.Persistence.Configurations;

public class PostLikeConfiguration : IEntityTypeConfiguration<PostLike>
{
    public void Configure(EntityTypeBuilder<PostLike> builder)
    {
        builder.ToTable("PostLikes");

        builder.HasKey(pl => pl.Id);

        builder.Property(pl => pl.LikeType)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("RedPill");

        // User relationship
        builder.HasOne(pl => pl.User)
            .WithMany()
            .HasForeignKey(pl => pl.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Post relationship
        builder.HasOne(pl => pl.Post)
            .WithMany(p => p.Likes)
            .HasForeignKey(pl => pl.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint - bir kullanıcı bir post'a sadece bir kez like atabilir
        builder.HasIndex(pl => new { pl.UserId, pl.PostId })
            .IsUnique();

        // Indexes for performance
        builder.HasIndex(pl => pl.PostId);
        builder.HasIndex(pl => pl.CreatedAt);
    }
}