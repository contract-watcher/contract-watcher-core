using ContractWatcher.Core.Data;
using ContractWatcher.Core.Data.Domain;
using ContractWatcher.Core.Models.Monitoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ContractWatcher.Core.Services.Monitoring;

/// <summary>
/// Доставка сохранённых отчётов в Monitoring. Гарантия at least once: отчёт отправляется, пока Monitoring его не примет,
/// а повторы Monitoring распознаёт по reportId. Отчёт, который Monitoring раз за разом не может обработать, пока
/// принимает остальные, откладывается после MaxDeliveryAttempts и раз в ParkedRetryHours получает ещё одну попытку.
/// Когда Monitoring недоступен целиком, предохранитель останавливает запросы, а попытки отчётов не расходуются
/// </summary>
public class ReportDeliveryService(
    DataContext db,
    MonitoringClient monitoringClient,
    MonitoringCircuitBreaker breaker,
    TimeProvider timeProvider,
    IOptions<MonitoringOptions> options,
    ILogger<ReportDeliveryService> logger
)
{
    /// <summary>
    /// Отправляет пачку отчётов, готовых к отправке, в порядке готовности.
    /// True — пачка была полной и в очереди могут остаться отчёты; false — очередь пуста или Monitoring недоступен
    /// </summary>
    public async Task<bool> DeliverBatchAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Предохранитель разомкнут: в Monitoring не ходим и очередь не читаем
        if (!breaker.TryEnter(now, out var probe))
            return false;

        // После паузы предохранителя — один пробный отчёт
        var take = probe ? 1 : options.Value.BatchSize;

        // Отчёт после неудачи встаёт в очередь по времени следующей попытки, а не по времени приёма.
        // Иначе отчёт, на котором Monitoring падает, снова оказывался бы первым и задерживал остальные
        var reports = await db
            .Reports.Include(x => x.Contract)
            .Where(x =>
                x.DeliveredAt == null
                && x.DeliveryError == null
                && (x.NextAttemptAt == null || x.NextAttemptAt <= now)
            )
            .OrderBy(x => x.NextAttemptAt ?? x.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        foreach (var report in reports)
        {
            // Неудача засчитывается отчёту, только если Monitoring в целом принимает отчёты
            var monitoringWasHealthy = breaker.IsClosed;

            var (status, error) = await monitoringClient.SendReportAsync(ToRequest(report), cancellationToken);
            now = timeProvider.GetUtcNow().UtcDateTime;

            switch (status)
            {
                case MonitoringDeliveryStatus.Delivered:
                    breaker.OnSuccess();
                    report.DeliveredAt = now;
                    break;

                case MonitoringDeliveryStatus.Rejected:
                    // Monitoring работает, плох сам отчёт
                    breaker.OnSuccess();
                    logger.LogError("Monitoring rejected report {ReportId}: {Error}", report.ReportId, error);
                    Park(report, error!, now);
                    break;

                case MonitoringDeliveryStatus.Failed:
                    breaker.OnFailure(now, error);
                    if (monitoringWasHealthy && breaker.IsClosed)
                        CountFailedAttempt(report, error!, now);
                    else
                        // Сбой похож на общий: попытку не тратим, но отчёт отодвигаем,
                        // чтобы следующий пробный запрос ушёл с другим отчётом
                        report.NextAttemptAt = now + RetryDelay(Math.Max(report.DeliveryAttempts, 1));
                    break;

                default:
                    // Monitoring недоступен: отчёт не виноват, остальные тоже не дойдут
                    breaker.OnFailure(now, error);
                    logger.LogDebug("Report delivery to Monitoring is postponed: {Error}", error);
                    return false;
            }

            // Сохраняем после каждого отчёта: при сбое посреди пачки уже доставленные не уйдут повторно
            await db.SaveChangesAsync(cancellationToken);

            if (!breaker.IsClosed)
                return false;
        }

        return reports.Count == take;
    }

    private void CountFailedAttempt(Report report, string error, DateTime now)
    {
        report.DeliveryAttempts++;

        if (report.DeliveryAttempts < options.Value.MaxDeliveryAttempts)
        {
            report.NextAttemptAt = now + RetryDelay(report.DeliveryAttempts);
            return;
        }

        logger.LogError(
            "Report {ReportId} is parked after {Attempts} failed delivery attempts: {Error}",
            report.ReportId,
            report.DeliveryAttempts,
            error
        );
        Park(report, error, now);
    }

    /// <summary>
    /// Возвращает в очередь отложенные отчёты, у которых подошло время ежедневной попытки: вдруг в Monitoring
    /// исправили ошибку. Попытка одна — отчёт, который снова не прошёл, откладывается ещё на ParkedRetryHours
    /// </summary>
    public async Task RequeueParkedAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // NextAttemptAt == null — отчёт отложен до появления ежедневного повтора, ему положена попытка сразу
        var parked = await db
            .Reports.Where(x => x.DeliveryError != null && (x.NextAttemptAt == null || x.NextAttemptAt <= now))
            .ToListAsync(cancellationToken);
        if (parked.Count == 0)
            return;

        foreach (var report in parked)
        {
            report.DeliveryError = null;
            // В конец очереди, за свежими отчётами. Счётчик попыток не сбрасывается: после 500 отчёт сразу
            // отложится снова, а не пройдёт новый цикл из MaxDeliveryAttempts попыток
            report.NextAttemptAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("{Count} parked reports are requeued for a daily delivery attempt", parked.Count);
    }

    // Отложенный отчёт выпадает из очереди до ежедневной попытки
    private void Park(Report report, string error, DateTime now)
    {
        report.DeliveryError = error.Length <= Report.DeliveryErrorMaxLength
            ? error
            : error[..Report.DeliveryErrorMaxLength];
        report.NextAttemptAt = now.AddHours(options.Value.ParkedRetryHours);
    }

    // RetryDelaySeconds × 2^(попытка − 1), не больше MaxRetryDelaySeconds, плюс разброс до 20 %,
    // чтобы отчёты, упавшие одновременно, не возвращались в одну и ту же секунду
    private TimeSpan RetryDelay(int attempts)
    {
        var seconds = Math.Min(
            options.Value.RetryDelaySeconds * Math.Pow(2, attempts - 1),
            options.Value.MaxRetryDelaySeconds
        );

        return TimeSpan.FromSeconds(seconds * (1 + Random.Shared.NextDouble() * 0.2));
    }

    private static MonitoringReportRequest ToRequest(Report report) =>
        new(
            report.Contract.IntegrationId,
            report.ContractId,
            new MonitoringReport(
                report.ReportId,
                report.ContractVersion,
                report.Status,
                report.Timestamp,
                report.Violations
            )
        );
}
