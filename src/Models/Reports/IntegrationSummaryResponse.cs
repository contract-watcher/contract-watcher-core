namespace ContractWatcher.Core.Models.Reports;

/// <summary>
/// Сводка по интеграции
/// </summary>
/// <param name="ReportCount">Всего проверок</param>
/// <param name="PassedCount">Проверок без нарушений</param>
/// <param name="FailedCount">Проверок с нарушениями</param>
/// <param name="ViolationGroupCount">Сколько всего групп нарушений</param>
/// <param name="LastReport">Последняя проверка. Null — проверок ещё не было</param>
/// <param name="LatestViolationGroups">Группы, которые встречались последними, новые первыми</param>
public record IntegrationSummaryResponse(
    long ReportCount,
    long PassedCount,
    long FailedCount,
    long ViolationGroupCount,
    ReportHistoryItemResponse? LastReport,
    List<ViolationGroupResponse> LatestViolationGroups
);
