using ContractWatcher.Core.Data.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractWatcher.Core.Data.Configurations;

public class EmailMessageConfiguration : IEntityTypeConfiguration<EmailMessage>
{
    public void Configure(EntityTypeBuilder<EmailMessage> builder)
    {
        builder.Property(x => x.To).HasMaxLength(EmailMessage.ToMaxLength);
        builder.Property(x => x.Subject).HasMaxLength(EmailMessage.SubjectMaxLength);
        builder.Property(x => x.LastError).HasMaxLength(EmailMessage.LastErrorMaxLength);

        // Очередь отправки: в индекс попадают только письма, которые ещё нужно отправить
        builder
            .HasIndex(x => x.NextAttemptAt)
            .HasDatabaseName("IX_EmailMessages_Pending")
            .HasFilter("\"SentAt\" IS NULL AND \"FailedAt\" IS NULL");
    }
}
