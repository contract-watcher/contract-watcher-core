using ContractWatcher.Core.Data.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractWatcher.Core.Data.Configurations;

public class ContractConfiguration : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> builder)
    {
        builder.Property(x => x.Slug).HasMaxLength(Contract.SlugMaxLength);
        builder.Property(x => x.Name).HasMaxLength(Contract.NameMaxLength);
        builder.Property(x => x.Description).HasMaxLength(Contract.DescriptionMaxLength);

        builder.HasIndex(x => new { x.IntegrationId, x.Slug }).IsUnique();

        builder
            .HasOne(x => x.Integration)
            .WithMany(x => x.Contracts)
            .HasForeignKey(x => x.IntegrationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
