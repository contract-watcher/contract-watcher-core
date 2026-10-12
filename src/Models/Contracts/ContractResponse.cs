namespace ContractWatcher.Core.Models.Contracts;

/// <summary>
/// Контракт в ответе API
/// </summary>
/// <param name="LatestVersion">Номер последней версии. Null — версий ещё нет</param>
public record ContractResponse(
    Guid Id,
    Guid IntegrationId,
    string Slug,
    string Name,
    string? Description,
    int? LatestVersion,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
