using Lexora.Domain.Conversations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lexora.Infrastructure.Persistence.Configurations;

internal sealed class ConversationMessageConfiguration : IEntityTypeConfiguration<ConversationMessage>
{
    public const string ConversationIdProperty = "ConversationId";

    public void Configure(EntityTypeBuilder<ConversationMessage> builder)
    {
        builder.ToTable(SchemaHelper.ConversationMessages);

        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).ValueGeneratedNever();

        builder.Property(message => message.Role).HasConversion<string>().HasMaxLength(ColumnLengths.EnumName);
        builder.Property(message => message.Provider).HasMaxLength(ColumnLengths.ProviderName);
        builder.Property(message => message.Model).HasMaxLength(ColumnLengths.ModelName);

        builder.ComplexProperty(message => message.Usage, usage =>
        {
            usage.Property(value => value.InputTokens).HasColumnName("input_tokens");
            usage.Property(value => value.OutputTokens).HasColumnName("output_tokens");
        });

        builder.OwnsMany(message => message.Citations, citations =>
        {
            citations.ToJson("citations");
            citations.Property(citation => citation.Index);
            citations.Property(citation => citation.ChunkId);
            citations.Property(citation => citation.DocumentId);
            citations.Property(citation => citation.DocumentName);
            citations.Property(citation => citation.PageNumber);
            citations.Property(citation => citation.SheetName);
            citations.Property(citation => citation.Excerpt);
            citations.Property(citation => citation.Score);
        });

        builder.Navigation(message => message.Citations).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(ConversationIdProperty, nameof(ConversationMessage.CreatedAt));
    }
}
