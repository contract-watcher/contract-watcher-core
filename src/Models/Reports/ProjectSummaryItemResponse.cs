namespace ContractWatcher.Core.Models.Reports;

/// <summary>
/// Сводка интеграции в списке проекта. Счётчики — из Monitoring за выбранный период, slug и название — из Core
/// </summary>
/// <param name="TotalReports">Проверок за период, равно PassedReports + FailedReports</param>
/// <param name="PassedReports">Проверок без нарушений за период</param>
/// <param name="FailedReports">Проверок с нарушениями за период</param>
/// <param name="LastReceivedAt">
/// Когда принят последний отчёт периода, UTC. Null — проверок в периоде нет: это не значит, что интеграция исправна
/// </param>
public record ProjectSummaryItemResponse(
    Guid IntegrationId,
    string Slug,
    string Name,
    long TotalReports,
    long PassedReports,
    long FailedReports,
    DateTime? LastReceivedAt
);
