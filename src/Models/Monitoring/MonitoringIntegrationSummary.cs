namespace ContractWatcher.Core.Models.Monitoring;

/// <summary>
/// Сводка интеграции из Monitoring: GET /internal/v1/integrations/{integrationId}/summary. Считается одним снимком базы
/// </summary>
/// <param name="TotalReports">Всего отчётов всех контрактов и версий, равно PassedReports + FailedReports</param>
/// <param name="LastReceivedAt">Когда Monitoring принял последний отчёт, UTC. Null — отчётов нет</param>
public record MonitoringIntegrationSummary(
    long TotalReports,
    long PassedReports,
    long FailedReports,
    DateTime? LastReceivedAt
);
