using System.Runtime.CompilerServices;
using Lexora.Domain.Chunks;
using Lexora.Domain.Documents;
using Lexora.Domain.KnowledgeBases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;
using Pgvector;

namespace Lexora.Infrastructure.Persistence.Configurations;

internal sealed class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public const string SearchVectorProperty = "SearchVector";

    // DocumentChunk replaces the whole array when it attaches an embedding and never mutates it in place,
    // so reference equality is enough and avoids comparing every vector element on each DetectChanges.
    private static readonly ValueComparer<float[]?> _embeddingComparer = new(
        (left, right) => ReferenceEquals(left, right),
        embedding => embedding == null ? 0 : RuntimeHelpers.GetHashCode(embedding),
        embedding => embedding);

    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.ToTable(SchemaHelper.DocumentChunks);

        builder.HasKey(chunk => chunk.Id);
        builder.Property(chunk => chunk.Id).ValueGeneratedNever();

        builder.Property(chunk => chunk.Index).HasColumnName("chunk_index");

        builder.ComplexProperty(chunk => chunk.Location, location =>
        {
            location.Property(value => value.PageNumber).HasColumnName("page_number");
            location.Property(value => value.SheetName).HasColumnName("sheet_name").HasMaxLength(ColumnLengths.SheetName);
            location.Property(value => value.SectionTitle).HasColumnName("section_title").HasMaxLength(ColumnLengths.SectionTitle);
            location.Property(value => value.StartOffset).HasColumnName("start_offset");
            location.Property(value => value.EndOffset).HasColumnName("end_offset");
        });

        builder.Property(chunk => chunk.Embedding)
            .HasColumnType($"vector({SchemaHelper.EmbeddingDimensions})")
            .HasConversion(
                embedding => embedding == null ? null : new Vector(embedding),
                vector => vector == null ? null : vector.ToArray(),
                _embeddingComparer);

        builder.HasIndex(chunk => chunk.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops")
            .HasStorageParameter("m", 16)
            .HasStorageParameter("ef_construction", 64);

        builder.Property(chunk => chunk.EmbeddingModel).HasMaxLength(ColumnLengths.ModelName);

        builder.Property<NpgsqlTsVector>(SearchVectorProperty)
            .HasComputedColumnSql("to_tsvector('portuguese', coalesce(section_title, '') || ' ' || content)", stored: true);

        builder.HasIndex(SearchVectorProperty).HasMethod("gin");

        builder.HasIndex(chunk => new { chunk.DocumentId, chunk.ProcessingVersion, chunk.Index }).IsUnique();
        builder.HasIndex(chunk => chunk.KnowledgeBaseId);

        builder.HasOne<Document>().WithMany().HasForeignKey(chunk => chunk.DocumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<KnowledgeBase>().WithMany().HasForeignKey(chunk => chunk.KnowledgeBaseId).OnDelete(DeleteBehavior.Restrict);
    }
}
