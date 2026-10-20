using ContractWatcher.Core.Data;
using ContractWatcher.Core.Data.Domain;
using ContractWatcher.Core.Models.Monitoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ContractWatcher.Core.Services.Monitoring;

/// <summary>
/// Доставка сохранённых отчётов в Monitoring. Гарантия at least once: отчёт отправляется, пока Monitoring его не примет,
/// а повторы Monitoring распознаёт по reportId
/// </summary>
public class ReportDeliveryService(
    DataContext db,
    MonitoringClient monitoringClient,
    TimeProvider timeProvider,
    IOptions<MonitoringOptions> options,
    ILogger<ReportDeliveryService> logger
)
{
    /// <summary>
    /// Отправляет пачку недоставленных отчётов, старые первыми.
    /// True — пачка была полной и в очереди могут остаться отчёты; false — очередь пуста или Monitoring недоступен
    /// </summary>
    public async Task<bool> DeliverBatchAsync(CancellationToken cancellationToken)
    {
        var batchSize = options.Value.BatchSize;
        var reports = await db
            .Reports.Include(x => x.Contract)
            .Where(x => x.DeliveredAt == null && x.DeliveryError == null)
            .OrderBy(x => x.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        foreach (var report in reports)
        {
            var request = new MonitoringReportRequest(
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

            var (status, error) = await monitoringClient.SendReportAsync(request, cancellationToken);
            if (status == MonitoringDeliveryStatus.Unavailable)
            {
                // Остальные отчёты пачки тоже не дойдут — они уйдут при следующем проходе
                logger.LogWarning("Report delivery to Monitoring is postponed: {Error}", error);
                return false;
            }

            if (status == MonitoringDeliveryStatus.Rejected)
            {
                logger.LogError("Monitoring rejected report {ReportId}: {Error}", report.ReportId, error);
                report.DeliveryError = error!.Length <= Report.DeliveryErrorMaxLength
                    ? error
                    : error[..Report.DeliveryErrorMaxLength];
            }
            else
            {
                report.DeliveredAt = timeProvider.GetUtcNow().UtcDateTime;
            }

            // Сохраняем после каждого отчёта: при сбое посреди пачки уже доставленные не уйдут повторно
            await db.SaveChangesAsync(cancellationToken);
        }

        return reports.Count == batchSize;
    }
}
