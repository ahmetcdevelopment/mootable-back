using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mootable.Domain.Entities;

namespace Mootable.Infrastructure.Persistence.Configurations;

public sealed class MootTableConfiguration : IEntityTypeConfiguration<MootTable>
{
    public void Configure(EntityTypeBuilder<MootTable> builder)
    {
        builder.ToTable("MootTables");

        builder.HasKey(mt => mt.Id);

        builder.Property(mt => mt.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(mt => mt.Topic)
            .HasMaxLength(1024);

        builder.HasIndex(mt => new { mt.ServerId, mt.Name })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasMany(mt => mt.Messages)
            .WithOne(m => m.MootTable)
            .HasForeignKey(m => m.MootTableId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(mt => mt.RabbitHoles)
            .WithOne(rh => rh.MootTable)
            .HasForeignKey(rh => rh.MootTableId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(mt => mt.Category)
            .WithMany(c => c.MootTables)
            .HasForeignKey(mt => mt.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
