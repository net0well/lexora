using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lexora.Infrastructure.Persistence.Configurations;

internal static class ConcurrencyTokenExtensions
{
    public const string PropertyName = "xmin";

    public static EntityTypeBuilder<TEntity> HasXminConcurrencyToken<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        builder.Property<uint>(PropertyName)
            .HasColumnName(PropertyName)
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        return builder;
    }
}
