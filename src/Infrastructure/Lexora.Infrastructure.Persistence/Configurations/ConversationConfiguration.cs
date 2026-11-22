using Lexora.Domain.Conversations;
using Lexora.Domain.KnowledgeBases;
using Lexora.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lexora.Infrastructure.Persistence.Configurations;

internal sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable(SchemaHelper.Conversations);

        builder.HasKey(conversation => conversation.Id);
        builder.Property(conversation => conversation.Id).ValueGeneratedNever();

        builder.Property(conversation => conversation.Title).HasMaxLength(Conversation.MaxTitleLength);

        builder.HasMany(conversation => conversation.Messages)
            .WithOne()
            .HasForeignKey(ConversationMessageConfiguration.ConversationIdProperty)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(conversation => conversation.Messages).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(conversation => new { conversation.UserId, conversation.UpdatedAt }).IsDescending(false, true);

        builder.HasOne<KnowledgeBase>().WithMany().HasForeignKey(conversation => conversation.KnowledgeBaseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(conversation => conversation.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
