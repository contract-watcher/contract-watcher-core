namespace ContractWatcher.Core.Models.Monitoring;

/// <summary>
/// Сводка интеграции из Monitoring: GET /internal/v1/integrations/{integrationId}/summary?from=&amp;to= и элемент
/// страницы POST /internal/v1/integration-summaries/query. Считается одним снимком базы за период [from, to), без
/// границ — за всё время
/// </summary>
/// <param name="TotalReports">Отчётов всех контрактов и версий за период, равно PassedReports + FailedReports</param>
/// <param name="LastReceivedAt">Когда принят последний отчёт периода, UTC. Null — отчётов в периоде нет</param>
public record MonitoringIntegrationSummary(
    Guid IntegrationId,
    long TotalReports,
    long PassedReports,
    long FailedReports,
    DateTime? LastReceivedAt
);
