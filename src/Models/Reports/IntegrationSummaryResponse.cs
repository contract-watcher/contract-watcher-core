namespace ContractWatcher.Core.Models.Reports;

/// <summary>
/// Сводка по интеграции. Счётчики отчётов — из сводки Monitoring за выбранный период, группы — из его списка групп
/// за всё время
/// </summary>
/// <param name="TotalReports">Проверок за период, равно PassedReports + FailedReports</param>
/// <param name="PassedReports">Проверок без нарушений за период</param>
/// <param name="FailedReports">Проверок с нарушениями за период</param>
/// <param name="LastReceivedAt">Когда принят последний отчёт периода, UTC. Null — проверок в периоде нет</param>
/// <param name="ViolationGroupCount">Сколько всего групп нарушений, без учёта периода</param>
/// <param name="LatestViolationGroups">Группы, которые встречались последними, новые первыми, без учёта периода</param>
public record IntegrationSummaryResponse(
    long TotalReports,
    long PassedReports,
    long FailedReports,
    DateTime? LastReceivedAt,
    long ViolationGroupCount,
    List<ViolationGroupResponse> LatestViolationGroups
);
