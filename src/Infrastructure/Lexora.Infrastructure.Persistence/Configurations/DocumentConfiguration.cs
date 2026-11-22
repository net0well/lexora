using Lexora.Domain.Documents;
using Lexora.Domain.KnowledgeBases;
using Lexora.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lexora.Infrastructure.Persistence.Configurations;

internal sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable(SchemaHelper.Documents);
        builder.HasXminConcurrencyToken();

        builder.HasKey(document => document.Id);
        builder.Property(document => document.Id).ValueGeneratedNever();

        builder.Property(document => document.FileName)
            .HasConversion(fileName => fileName.Value, value => FileName.Create(value).Value)
            .HasMaxLength(FileName.MaxLength);

        builder.Property(document => document.ContentHash)
            .HasConversion(hash => hash.Value, value => ContentHash.FromHex(value))
            .HasMaxLength(ContentHash.Length)
            .IsFixedLength();

        builder.Property(document => document.Format).HasConversion<string>().HasMaxLength(ColumnLengths.EnumName);
        builder.Property(document => document.Status).HasConversion<string>().HasMaxLength(ColumnLengths.EnumName);
        builder.Property(document => document.ContentType).HasMaxLength(ColumnLengths.ContentType);
        builder.Property(document => document.StorageKey).HasMaxLength(ColumnLengths.StorageKey);
        builder.Property(document => document.ExtractedTextKey).HasMaxLength(ColumnLengths.StorageKey);
        builder.Property(document => document.EmbeddingModel).HasMaxLength(ColumnLengths.ModelName);
        builder.Property(document => document.FailureReason).HasMaxLength(ColumnLengths.FailureReason);

        builder.HasIndex(document => new { document.KnowledgeBaseId, document.Status });

        builder.HasIndex(document => new { document.KnowledgeBaseId, document.ContentHash })
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        builder.HasOne<KnowledgeBase>().WithMany().HasForeignKey(document => document.KnowledgeBaseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(document => document.UploadedBy).OnDelete(DeleteBehavior.Restrict);
    }
}
