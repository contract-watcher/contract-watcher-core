using ContractWatcher.Core.Data.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractWatcher.Core.Data.Configurations;

public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(100);
        builder.Property(x => x.KeyHash).HasMaxLength(64);
        builder.Property(x => x.KeyHint).HasMaxLength(8);

        builder.HasIndex(x => x.KeyHash).IsUnique();

        builder
            .HasOne(x => x.Project)
            .WithMany(x => x.ApiKeys)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
