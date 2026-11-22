using Lexora.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lexora.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable(SchemaHelper.Users);

        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).ValueGeneratedNever();

        builder.Property(user => user.Email)
            .HasConversion(email => email.Value, value => Email.Create(value).Value)
            .HasMaxLength(Email.MaxLength);

        builder.HasIndex(user => user.Email).IsUnique();

        builder.Property(user => user.Name)
            .HasConversion(name => name.Value, value => PersonName.Create(value).Value)
            .HasMaxLength(PersonName.MaxLength);

        builder.Property(user => user.Role).HasConversion<string>().HasMaxLength(ColumnLengths.EnumName);
        builder.Property(user => user.Status).HasConversion<string>().HasMaxLength(ColumnLengths.EnumName);
    }
}
