using ContractWatcher.Common.Enums;

namespace ContractWatcher.Core.Models.Reports;

/// <summary>
/// Отчёт проверки в истории
/// </summary>
/// <param name="ContractSlug">Slug контракта. Null — контракт удалён из Core, а история в Monitoring осталась</param>
/// <param name="ContractName">Название контракта. Null — контракт удалён из Core</param>
/// <param name="ReceivedAt">Когда Monitoring принял отчёт, UTC</param>
public record ReportHistoryItemResponse(
    Guid ReportId,
    Guid ContractId,
    string? ContractSlug,
    string? ContractName,
    int ContractVersion,
    ValidationStatus Status,
    DateTime ReceivedAt,
    long ViolationCount
);
