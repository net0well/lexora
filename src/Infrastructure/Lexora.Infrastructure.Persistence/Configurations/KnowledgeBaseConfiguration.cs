using Lexora.Domain.KnowledgeBases;
using Lexora.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lexora.Infrastructure.Persistence.Configurations;

internal sealed class KnowledgeBaseConfiguration : IEntityTypeConfiguration<KnowledgeBase>
{
    public const string NormalizedNameProperty = "NormalizedName";

    public void Configure(EntityTypeBuilder<KnowledgeBase> builder)
    {
        builder.ToTable(SchemaHelper.KnowledgeBases);
        builder.HasXminConcurrencyToken();

        builder.HasKey(knowledgeBase => knowledgeBase.Id);
        builder.Property(knowledgeBase => knowledgeBase.Id).ValueGeneratedNever();

        builder.Property(knowledgeBase => knowledgeBase.Name)
            .HasConversion(name => name.Value, value => KnowledgeBaseName.Create(value).Value)
            .HasMaxLength(KnowledgeBaseName.MaxLength);

        builder.Property<string>(NormalizedNameProperty)
            .HasMaxLength(KnowledgeBaseName.MaxLength)
            .HasComputedColumnSql("lower(name)", stored: true);

        builder.HasIndex(NormalizedNameProperty).IsUnique();

        builder.Property(knowledgeBase => knowledgeBase.Description).HasMaxLength(KnowledgeBase.DescriptionMaxLength);

        builder.ComplexProperty(knowledgeBase => knowledgeBase.Chunking, chunking =>
        {
            chunking.Property(settings => settings.MaxTokens).HasColumnName("chunk_max_tokens");
            chunking.Property(settings => settings.OverlapTokens).HasColumnName("chunk_overlap_tokens");
        });

        builder.Property(knowledgeBase => knowledgeBase.Status).HasConversion<string>().HasMaxLength(ColumnLengths.EnumName);

        builder.HasOne<User>().WithMany().HasForeignKey(knowledgeBase => knowledgeBase.CreatedBy).OnDelete(DeleteBehavior.Restrict);
    }
}
