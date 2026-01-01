using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mootable.Domain.Entities;

namespace Mootable.Infrastructure.Persistence.Configurations;

public sealed class ServerConfiguration : IEntityTypeConfiguration<Server>
{
    public void Configure(EntityTypeBuilder<Server> builder)
    {
        builder.ToTable("Servers");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.Description)
            .HasMaxLength(1000);

        builder.Property(s => s.IconUrl)
            .HasMaxLength(512);

        builder.Property(s => s.InviteCode)
            .HasMaxLength(16)
            .IsRequired();

        builder.HasIndex(s => s.InviteCode)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasMany(s => s.Members)
            .WithOne(sm => sm.Server)
            .HasForeignKey(sm => sm.ServerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.MootTables)
            .WithOne(mt => mt.Server)
            .HasForeignKey(mt => mt.ServerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.ServerRoles)
            .WithOne(sr => sr.Server)
            .HasForeignKey(sr => sr.ServerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
