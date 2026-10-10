namespace ContractWatcher.Core.Models.Contracts;

/// <summary>
/// Версия контракта в ответе API
/// </summary>
public record ContractVersionResponse(
    Guid Id,
    Guid ContractId,
    int Version,
    string Schema,
    string? Comment,
    DateTime CreatedAt
);
