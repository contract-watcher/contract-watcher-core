using ContractWatcher.Common.Enums;

namespace ContractWatcher.Core.Models.Reports;

/// <summary>
/// Группа одинаковых нарушений. Группа относится к одной версии контракта
/// </summary>
/// <param name="ContractSlug">Slug контракта. Null — контракт удалён из Core</param>
/// <param name="ContractName">Название контракта. Null — контракт удалён из Core</param>
/// <param name="OccurrenceCount">Сколько раз нарушение встретилось</param>
/// <param name="FirstSeenAt">Первое появление, UTC</param>
/// <param name="LastSeenAt">Последнее появление, UTC</param>
public record ViolationGroupResponse(
    Guid GroupId,
    Guid ContractId,
    string? ContractSlug,
    string? ContractName,
    int ContractVersion,
    string FieldName,
    ViolationType Type,
    ContractFieldType? ExpectedType,
    ContractFieldType? ActualType,
    long OccurrenceCount,
    DateTime FirstSeenAt,
    DateTime LastSeenAt
);
