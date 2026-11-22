using Lexora.Domain.RefreshTokens;
using Lexora.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lexora.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable(SchemaHelper.RefreshTokens);

        builder.HasKey(token => token.Id);
        builder.Property(token => token.Id).ValueGeneratedNever();

        builder.Property(token => token.TokenHash).HasMaxLength(ColumnLengths.Hash);
        builder.HasIndex(token => token.TokenHash).IsUnique();

        builder.Property(token => token.RevocationReason).HasConversion<string>().HasMaxLength(ColumnLengths.EnumName);

        builder.HasIndex(token => token.UserId);
        builder.HasIndex(token => token.FamilyId);

        builder.HasOne<User>().WithMany().HasForeignKey(token => token.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
