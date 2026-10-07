using ContractWatcher.Core.Data.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractWatcher.Core.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(x => x.Email).HasMaxLength(User.EmailMaxLength);
        builder.Property(x => x.PasswordHash).HasMaxLength(256);
        builder.Property(x => x.Name).HasMaxLength(User.NameMaxLength);

        builder.HasIndex(x => x.Email).IsUnique();
    }
}
