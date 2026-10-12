using System.Text.Json;
using ContractWatcher.Common.Contracts;
using ContractWatcher.Core.Data.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractWatcher.Core.Data.Configurations;

public class ContractVersionConfiguration : IEntityTypeConfiguration<ContractVersion>
{
    // Правила хранятся в том же JSON-формате, что и в API: camelCase, тип поля строкой
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<ContractVersion> builder)
    {
        builder
            .Property(x => x.Rules)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<List<ContractRule>>(v, JsonOptions)!,
                new ValueComparer<List<ContractRule>>(
                    (a, b) => JsonSerializer.Serialize(a, JsonOptions) == JsonSerializer.Serialize(b, JsonOptions),
                    v => JsonSerializer.Serialize(v, JsonOptions).GetHashCode(),
                    v => JsonSerializer.Deserialize<List<ContractRule>>(
                        JsonSerializer.Serialize(v, JsonOptions),
                        JsonOptions
                    )!
                )
            );
        builder.Property(x => x.Comment).HasMaxLength(ContractVersion.CommentMaxLength);

        builder.HasIndex(x => new { x.ContractId, x.Version }).IsUnique();

        builder
            .HasOne(x => x.Contract)
            .WithMany(x => x.Versions)
            .HasForeignKey(x => x.ContractId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
