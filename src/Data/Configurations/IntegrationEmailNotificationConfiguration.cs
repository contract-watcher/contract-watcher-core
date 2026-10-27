using ContractWatcher.Core.Data.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractWatcher.Core.Data.Configurations;

public class IntegrationEmailNotificationConfiguration : IEntityTypeConfiguration<IntegrationEmailNotification>
{
    public void Configure(EntityTypeBuilder<IntegrationEmailNotification> builder)
    {
        // Не больше одной записи на интеграцию: параллельное включение уведомлений падает на этом индексе
        builder.HasIndex(x => x.IntegrationId).IsUnique();

        builder
            .HasOne(x => x.Integration)
            .WithOne()
            .HasForeignKey<IntegrationEmailNotification>(x => x.IntegrationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
