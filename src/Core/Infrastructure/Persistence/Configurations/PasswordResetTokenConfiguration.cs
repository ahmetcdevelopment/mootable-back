using Mootable.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mootable.Infrastructure.Persistence.Configurations;

/// <summary>
/// Password reset token entity configuration
/// </summary>
public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("PasswordResetTokens");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Token)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.RequestedFromIP)
            .HasMaxLength(50);

        builder.Property(e => e.RequestedUserAgent)
            .HasMaxLength(500);

        // Indexes
        builder.HasIndex(e => e.Token)
            .IsUnique()
            .HasDatabaseName("IX_PasswordResetTokens_Token");

        builder.HasIndex(e => e.Email)
            .HasDatabaseName("IX_PasswordResetTokens_Email");

        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_PasswordResetTokens_UserId");

        builder.HasIndex(e => new { e.UserId, e.IsUsed, e.ExpiresAt })
            .HasDatabaseName("IX_PasswordResetTokens_UserId_IsUsed_ExpiresAt");

        // Relationships
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}