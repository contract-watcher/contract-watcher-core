using ContractWatcher.Core.Data.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractWatcher.Core.Data.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(Project.NameMaxLength);

        builder
            .HasOne(x => x.Owner)
            .WithMany(x => x.Projects)
            .HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
