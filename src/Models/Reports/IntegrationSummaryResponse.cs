namespace ContractWatcher.Core.Models.Reports;

/// <summary>
/// Сводка по интеграции. Счётчики отчётов — из сводки Monitoring, группы — из его списка групп
/// </summary>
/// <param name="TotalReports">Всего проверок, равно PassedReports + FailedReports</param>
/// <param name="PassedReports">Проверок без нарушений</param>
/// <param name="FailedReports">Проверок с нарушениями</param>
/// <param name="LastReceivedAt">Когда Monitoring принял последний отчёт, UTC. Null — проверок ещё не было</param>
/// <param name="ViolationGroupCount">Сколько всего групп нарушений</param>
/// <param name="LatestViolationGroups">Группы, которые встречались последними, новые первыми</param>
public record IntegrationSummaryResponse(
    long TotalReports,
    long PassedReports,
    long FailedReports,
    DateTime? LastReceivedAt,
    long ViolationGroupCount,
    List<ViolationGroupResponse> LatestViolationGroups
);
