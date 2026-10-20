using ContractWatcher.Common.Enums;

namespace ContractWatcher.Core.Models.Reports;

/// <summary>
/// Подробности отчёта проверки. Исходного JSON нет: в отчёт попадают только имена полей и типы
/// </summary>
/// <param name="ContractSlug">Slug контракта. Null — контракт удалён из Core</param>
/// <param name="ContractName">Название контракта. Null — контракт удалён из Core</param>
/// <param name="ReceivedAt">Когда Monitoring принял отчёт, UTC</param>
/// <param name="Violations">Нарушения в исходном порядке, одинаковые не объединяются</param>
public record ReportDetailsResponse(
    Guid ReportId,
    Guid ContractId,
    string? ContractSlug,
    string? ContractName,
    int ContractVersion,
    ValidationStatus Status,
    DateTime ReceivedAt,
    long ViolationCount,
    List<ReportViolationResponse> Violations
);

/// <summary>
/// Нарушение в отчёте
/// </summary>
/// <param name="GroupId">Группа одинаковых нарушений в Monitoring</param>
public record ReportViolationResponse(
    Guid? GroupId,
    string FieldName,
    ViolationType Type,
    ContractFieldType? ExpectedType,
    ContractFieldType? ActualType
);
