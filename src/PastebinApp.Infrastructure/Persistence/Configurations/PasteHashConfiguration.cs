using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PastebinApp.Domain.Entities;

namespace PastebinApp.Infrastructure.Persistence.Configurations;

public class PasteHashConfiguration : IEntityTypeConfiguration<PasteHash>
{
    public void Configure(EntityTypeBuilder<PasteHash> builder)
    {
        builder.ToTable("paste_hashes");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id)
            .HasColumnName("id")
            .UseIdentityColumn();

        builder.Property(h => h.Hash)
            .HasColumnName("hash")
            .HasMaxLength(12)
            .IsRequired();

        builder.HasIndex(h => h.Hash)
            .IsUnique()
            .HasDatabaseName("ix_paste_hashes_hash");

        builder.Property(h => h.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
    }
}