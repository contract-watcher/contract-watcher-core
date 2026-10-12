using ContractWatcher.Common.Contracts;

namespace ContractWatcher.Core.Models.Contracts;

/// <summary>
/// Версия контракта в ответе API
/// </summary>
/// <param name="PublishedAt">Дата публикации. Null — версия не опубликована</param>
public record ContractVersionResponse(
    Guid Id,
    Guid ContractId,
    int Version,
    List<ContractRule> Rules,
    string? Comment,
    DateTime CreatedAt,
    DateTime? PublishedAt
);
