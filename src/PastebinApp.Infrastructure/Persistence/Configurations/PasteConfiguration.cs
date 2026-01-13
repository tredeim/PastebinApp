using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PastebinApp.Domain.Entities;

namespace PastebinApp.Infrastructure.Persistence.Configurations;

public class PasteConfiguration : IEntityTypeConfiguration<Paste>
{
    public void Configure(EntityTypeBuilder<Paste> builder)
    {
        builder.ToTable("pastes");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(p => p.Hash)
            .HasColumnName("hash")
            .HasMaxLength(12)
            .IsRequired();

        builder.HasIndex(p => p.Hash)
            .IsUnique()
            .HasDatabaseName("ix_pastes_hash");

        builder.Property(p => p.ContentSizeBytes)
            .HasColumnName("content_size_bytes")
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(p => p.CreatedAt)
            .HasDatabaseName("ix_pastes_created_at");

        builder.Property(p => p.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.HasIndex(p => p.ExpiresAt)
            .HasDatabaseName("ix_pastes_expires_at");

        builder.Property(p => p.ViewCount)
            .HasColumnName("view_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(p => p.Language)
            .HasColumnName("language")
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(p => p.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired(false);
        
        builder.HasIndex(p => new { p.ExpiresAt, p.Id })
            .HasDatabaseName("ix_pastes_expires_at_id");
    }
}