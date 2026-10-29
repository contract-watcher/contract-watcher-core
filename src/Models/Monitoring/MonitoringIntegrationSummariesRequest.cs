using System.Text.Json.Serialization;

namespace ContractWatcher.Core.Models.Monitoring;

/// <summary>
/// Запрос сводок набора интеграций в Monitoring: POST /internal/v1/integration-summaries/query. Monitoring отклоняет
/// явный null, поэтому граница периода без значения не передаётся
/// </summary>
/// <param name="IntegrationIds">Все интеграции проекта: сортировку и страницу Monitoring считает по всему набору</param>
/// <param name="From">Нижняя граница, включительно: UTC с Z и дробью до шести знаков</param>
/// <param name="To">Верхняя граница, исключительно: UTC с Z и дробью до шести знаков</param>
public record MonitoringIntegrationSummariesRequest(
    List<Guid> IntegrationIds,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? From,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? To,
    bool OnlyFailed,
    int Offset,
    int Limit
);
