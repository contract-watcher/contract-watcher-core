using ContractWatcher.Core.Data.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractWatcher.Core.Data.Configurations;

public class IntegrationConfiguration : IEntityTypeConfiguration<Integration>
{
    public void Configure(EntityTypeBuilder<Integration> builder)
    {
        builder.Property(x => x.Slug).HasMaxLength(100);
        builder.Property(x => x.Name).HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(1000);

        builder.HasIndex(x => new { x.ProjectId, x.Slug }).IsUnique();

        builder
            .HasOne(x => x.Project)
            .WithMany(x => x.Integrations)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
