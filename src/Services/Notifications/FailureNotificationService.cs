using ContractWatcher.Core.Data;
using ContractWatcher.Core.Data.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ContractWatcher.Core.Services.Notifications;

/// <summary>
/// Письма владельцу проекта о неуспешных проверках. Повод — неуспешный отчёт, который Monitoring подтвердил,
/// поэтому к моменту письма отчёт уже есть в истории. Первая неуспешная проверка вызывает письмо сразу,
/// следующие собираются в сводное письмо по интеграции не чаще раза в DigestIntervalMinutes.
/// Нет новых ошибок — нет писем
/// </summary>
public class FailureNotificationService(
    DataContext db,
    TimeProvider timeProvider,
    IOptions<NotificationOptions> options,
    ILogger<FailureNotificationService> logger
)
{
    // У скольких последних проверок разбирать нарушения для причин в письме. Счётчики считаются по всем
    private const int ViolationSampleSize = 100;

    /// <summary>
    /// Ставит в очередь письма по интеграциям, у которых есть неуспешные проверки и подошло время письма
    /// </summary>
    public async Task ComposeDueAsync(CancellationToken cancellationToken)
    {
        var integrationIds = await db
            .Reports.Where(x => x.FailureNotificationPending)
            .Select(x => x.Contract.IntegrationId)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var integrationId in integrationIds)
            await ComposeAsync(integrationId, cancellationToken);
    }

    private async Task ComposeAsync(Guid integrationId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var pending = db.Reports.Where(x => x.FailureNotificationPending && x.Contract.IntegrationId == integrationId);

        var target = await db
            .IntegrationEmailNotifications.Where(x => x.IntegrationId == integrationId)
            .Select(x => new
            {
                x.LastEmailAt,
                x.Integration.ProjectId,
                IntegrationName = x.Integration.Name,
                ProjectName = x.Integration.Project.Name,
                x.Integration.Project.Owner.Email,
                x.Integration.Project.Owner.EmailConfirmedAt,
            })
            .SingleOrDefaultAsync(cancellationToken);

        // Уведомления выключили, пока отчёты ждали письма, или адрес владельца не подтверждён: письма не будет
        if (target?.EmailConfirmedAt is null)
        {
            await ClearPendingAsync(pending, now, cancellationToken);
            return;
        }

        // Письмо уже было недавно: ошибки копятся до сводного письма
        if (target.LastEmailAt > now.AddMinutes(-options.Value.DigestIntervalMinutes))
            return;

        var checks = await pending
            .OrderByDescending(x => x.Timestamp)
            .Select(x => new FailedCheck(x.Id, x.ContractId, x.Contract.Name, x.Contract.Slug, x.Timestamp))
            .ToListAsync(cancellationToken);
        if (checks.Count == 0)
            return;

        var sampleIds = checks.Take(ViolationSampleSize).Select(x => x.ReportId).ToList();
        var samples = await db
            .Reports.Where(x => sampleIds.Contains(x.Id))
            .Select(x => new FailedCheckViolations(x.Contract.Name, x.ContractVersion, x.Violations))
            .ToListAsync(cancellationToken);

        var link = FrontendLinks.Integration(options.Value.FrontendBaseUrl, target.ProjectId, integrationId);
        var (subject, body) = FailureEmail.Compose(target.ProjectName, target.IntegrationName, link, checks, samples);

        // Письмо, отметка времени и снятие флагов — одной транзакцией: письмо не задвоится и не потеряется
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Условие на прежнее LastEmailAt: из двух экземпляров Core письмо поставит только первый.
        // ExecuteUpdate идёт мимо TimestampInterceptor, поэтому UpdatedAt ставим сами
        var claimed = await db
            .IntegrationEmailNotifications.Where(x => x.IntegrationId == integrationId && x.LastEmailAt == target.LastEmailAt)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.LastEmailAt, now).SetProperty(x => x.UpdatedAt, now),
                cancellationToken
            );
        if (claimed == 0)
            return;

        db.EmailMessages.Add(
            new EmailMessage
            {
                To = target.Email,
                Subject = subject.Length <= EmailMessage.SubjectMaxLength ? subject : subject[..EmailMessage.SubjectMaxLength],
                Body = body,
                NextAttemptAt = now,
            }
        );
        await db.SaveChangesAsync(cancellationToken);

        // Снимаем флаг только у проверок, вошедших в письмо: пришедшие за это время попадут в следующее
        var reportIds = checks.Select(x => x.ReportId).ToList();
        await ClearPendingAsync(db.Reports.Where(x => reportIds.Contains(x.Id)), now, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation(
            "Failure email for integration {IntegrationId} is queued: {Count} failed checks",
            integrationId,
            checks.Count
        );
    }

    private static Task<int> ClearPendingAsync(IQueryable<Report> reports, DateTime now, CancellationToken cancellationToken) =>
        reports.ExecuteUpdateAsync(
            setters => setters.SetProperty(x => x.FailureNotificationPending, false).SetProperty(x => x.UpdatedAt, now),
            cancellationToken
        );
}
