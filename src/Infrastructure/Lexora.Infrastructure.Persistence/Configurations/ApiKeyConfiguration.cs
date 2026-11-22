using Lexora.Domain.ApiKeys;
using Lexora.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lexora.Infrastructure.Persistence.Configurations;

internal sealed class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable(SchemaHelper.ApiKeys);

        builder.HasKey(apiKey => apiKey.Id);
        builder.Property(apiKey => apiKey.Id).ValueGeneratedNever();

        builder.Property(apiKey => apiKey.Name).HasMaxLength(ApiKey.NameMaxLength);

        builder.Property(apiKey => apiKey.Prefix).HasMaxLength(ApiKey.PrefixLength).IsFixedLength();
        builder.HasIndex(apiKey => apiKey.Prefix).IsUnique();

        builder.Property(apiKey => apiKey.KeyHash).HasMaxLength(ColumnLengths.Hash);
        builder.Property(apiKey => apiKey.Role).HasConversion<string>().HasMaxLength(ColumnLengths.EnumName);

        builder.HasOne<User>().WithMany().HasForeignKey(apiKey => apiKey.CreatedBy).OnDelete(DeleteBehavior.Restrict);
    }
}
