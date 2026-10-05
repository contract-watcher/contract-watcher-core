using ContractWatcher.Core.Data.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractWatcher.Core.Data.Configurations;

public class ContractVersionConfiguration : IEntityTypeConfiguration<ContractVersion>
{
    public void Configure(EntityTypeBuilder<ContractVersion> builder)
    {
        builder.Property(x => x.Schema).HasColumnType("jsonb");
        builder.Property(x => x.Comment).HasMaxLength(500);

        builder.HasIndex(x => new { x.ContractId, x.Version }).IsUnique();

        builder
            .HasOne(x => x.Contract)
            .WithMany(x => x.Versions)
            .HasForeignKey(x => x.ContractId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
