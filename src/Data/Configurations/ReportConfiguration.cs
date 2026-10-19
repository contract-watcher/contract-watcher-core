using System.Text.Json;
using ContractWatcher.Common.Contracts;
using ContractWatcher.Core.Data.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractWatcher.Core.Data.Configurations;

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    // Нарушения хранятся в том же JSON-формате, что и в API: camelCase, типы строкой
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<Report> builder)
    {
        // Статус строкой, как в API: значение в базе не зависит от порядка элементов enum
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder
            .Property(x => x.Violations)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<List<ValidationViolation>>(v, JsonOptions)!,
                new ValueComparer<List<ValidationViolation>>(
                    (a, b) => JsonSerializer.Serialize(a, JsonOptions) == JsonSerializer.Serialize(b, JsonOptions),
                    v => JsonSerializer.Serialize(v, JsonOptions).GetHashCode(),
                    v => JsonSerializer.Deserialize<List<ValidationViolation>>(
                        JsonSerializer.Serialize(v, JsonOptions),
                        JsonOptions
                    )!
                )
            );

        // Ключ идемпотентности: повторная вставка того же отчёта падает на этом индексе
        builder.HasIndex(x => x.ReportId).IsUnique();

        builder
            .HasOne(x => x.Contract)
            .WithMany()
            .HasForeignKey(x => x.ContractId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
