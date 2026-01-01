using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mootable.Domain.Entities;

namespace Mootable.Infrastructure.Persistence.Configurations;

public sealed class RabbitHoleConfiguration : IEntityTypeConfiguration<RabbitHole>
{
    public void Configure(EntityTypeBuilder<RabbitHole> builder)
    {
        builder.ToTable("RabbitHoles");

        builder.HasKey(rh => rh.Id);

        builder.Property(rh => rh.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(rh => rh.StarterMessageId)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasMany(rh => rh.Messages)
            .WithOne(m => m.RabbitHole)
            .HasForeignKey(m => m.RabbitHoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rh => rh.StarterMessage)
            .WithMany()
            .HasForeignKey(rh => rh.StarterMessageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
