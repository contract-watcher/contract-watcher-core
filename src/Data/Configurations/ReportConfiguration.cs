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

        builder.Property(x => x.DeliveryError).HasMaxLength(Report.DeliveryErrorMaxLength);

        // Ключ идемпотентности: повторная вставка того же отчёта падает на этом индексе
        builder.HasIndex(x => x.ReportId).IsUnique();

        // Очередь доставки в Monitoring: в индекс попадают только отчёты, которые ещё нужно отправить
        builder
            .HasIndex(x => x.CreatedAt)
            .HasDatabaseName("IX_Reports_PendingDelivery")
            .HasFilter("\"DeliveredAt\" IS NULL AND \"DeliveryError\" IS NULL");

        // Отложенные отчёты для ежедневного повтора: их мало, проверка каждого прохода worker'а не читает всю таблицу
        builder
            .HasIndex(x => x.NextAttemptAt)
            .HasDatabaseName("IX_Reports_Parked")
            .HasFilter("\"DeliveryError\" IS NOT NULL");

        // Неуспешные отчёты, которые ждут письма: их мало, проход worker'а уведомлений не читает всю таблицу.
        // Индекс именованный, иначе он слился бы с другим индексом на тех же столбцах, и не по ContractId:
        // такой индекс EF считает покрывающим внешний ключ и удаляет обычный IX_Reports_ContractId
        builder
            .HasIndex(x => x.Timestamp, "IX_Reports_FailureNotificationPending")
            .HasFilter("\"FailureNotificationPending\"");

        builder
            .HasOne(x => x.Contract)
            .WithMany()
            .HasForeignKey(x => x.ContractId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
